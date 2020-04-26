﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeleniumBrowsersPool.BrowserPool.Factories;
using SeleniumBrowsersPool.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public class BrowserPoolArbitrator : IBrowserPoolArbitrator, IDisposable
    {
        private readonly IBrowserPoolInternal _browserPool;
        private readonly IBrowserFactory _factory;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOptions<BrowserPoolSettings> _poolSettings;
        private readonly ILogger<BrowserPoolArbitrator> _logger;

        private CancellationTokenSource tokenSource;
        private List<BrowserWrapper> browsers = new List<BrowserWrapper>();

        public BrowserPoolArbitrator(IBrowserPoolInternal browserPool,
                                     IBrowserFactory factory,
                                     IDateTimeProvider dateTimeProvider,
                                     IOptions<BrowserPoolSettings> poolSettings,
                                     ILogger<BrowserPoolArbitrator> logger)
        {
            _browserPool = browserPool;
            _factory = factory;
            _dateTimeProvider = dateTimeProvider;
            _poolSettings = poolSettings;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            tokenSource = new CancellationTokenSource();
            var maxIdleTime = _poolSettings.Value.MaxIdleTime;
            var deltaIdleTime = _poolSettings.Value.DeltaIdleTime.Ticks;

            if (_poolSettings.Value.StartBrowsersOnRun)
                browsers = Enumerable
                    .Range(0, _poolSettings.Value.MaxDegreeOfParallel)
                    .Select(i =>
                    {
                        var browser = new BrowserWrapper(
                            _factory.Create(),
                            maxIdleTime + TimeSpan.FromTicks(deltaIdleTime * i),
                            _dateTimeProvider.UtcNow);
                        _logger.LogDebug("Start browser {Id}", browser._id);
                        return browser;
                    })
                    .ToList();

            Task.Run(() => Loop(tokenSource.Token))
                .ContinueWith(t => _logger.LogError(t.Exception, "loopTask failed with exception"), TaskContinuationOptions.OnlyOnFaulted);
            _logger.LogDebug("Loop started");
            return _browserPool.StartAsync(browsers);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stop started");
            tokenSource.Cancel();
            StopBrowsers();
            _logger.LogDebug("All browsers been leved");
            await _browserPool.StopAsync();
            isDisposed = true;
        }

        private async Task Loop(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                var browserToStop = browsers.FirstOrDefault(x => x.CanBeStopped && x._onWork != true);
                if (browserToStop != null)
                {
                    browsers.Remove(browserToStop);
                    browserToStop._driver.Quit();
                }

                if (!_poolSettings.Value.KeepAliveAtLeastOneBrowser
                    || browsers.Where(x => !x.CanBeStopped).Count() > 1)
                {
                    browserToStop = browsers
                        .FirstOrDefault(b => b.LastJobTime == browsers
                            .Where(x => x.LastJobTime != default && !x._onWork && !x.CanBeStopped)
                            .Where(x => _dateTimeProvider.UtcNow - x.LastJobTime > x._maxIdleTime)
                            .NullIfEmpty()
                            ?.Min(x => x.LastJobTime));
                    if (browserToStop != null)
                        browserToStop.CanBeStopped = true;
                }

                await StartBrowser(token);
                Thread.Sleep(200);
            }
        }

        private async Task StartBrowser(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            var activeBrowsers = browsers.Where(x => !x.CanBeStopped).Count();
            if (activeBrowsers < _poolSettings.Value.MaxDegreeOfParallel
                && activeBrowsers * _poolSettings.Value.MaxQueueSizePerBrowser < await _browserPool.GetQueueLength()
                || (activeBrowsers < 1 && _poolSettings.Value.KeepAliveAtLeastOneBrowser))
            {
                var browser = new BrowserWrapper(
                _factory.Create(),
                //degradation point
                browsers.LastOrDefault()?._maxIdleTime ?? _poolSettings.Value.MaxIdleTime + TimeSpan.FromTicks(_poolSettings.Value.DeltaIdleTime.Ticks),
                _dateTimeProvider.UtcNow);

                browsers.Add(browser);
                if (token.IsCancellationRequested)
                {
                    browser._driver.Quit();
                    return;
                }

                await _browserPool.RegisterBrowser(browser);
                return;
            }
        }

        private void StopBrowsers()
            => browsers.ForEach(b =>
            {
                try
                {
                    var handler = b._driver.CurrentWindowHandle;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Session can't be closed automatically.");
                }

                b._driver.Quit();
                return;
            });

        #region IDisposable Support
        private bool isDisposed = false;

        protected virtual void Dispose(bool _)
        {
            if (!isDisposed)
                StopAsync(new CancellationToken()).Wait();
            else
                _logger.LogInformation("Already disposed");
        }

        ~BrowserPoolArbitrator()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}