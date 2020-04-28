using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeleniumBrowsersPool.BrowserPool.Commands;
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
            isDisposed = false;
            tokenSource = new CancellationTokenSource();
            var maxIdleTime = _poolSettings.Value.MaxIdleTime;
            var deltaIdleTime = _poolSettings.Value.DeltaIdleTime.Ticks;

            if (_poolSettings.Value.StartBrowsersOnRun)
                browsers = Enumerable
                    .Range(0, _poolSettings.Value.MaxDegreeOfParallel)
                    .Select(i =>
                    {
                        var browser = StartBrowserSafe(maxIdleTime + TimeSpan.FromTicks(deltaIdleTime * i));
                        if (browser != null)
                            _logger.LogDebug("Start browser {Id}", browser._id);
                        return browser;
                    })
                    .Where(x => x != null)
                    .ToList();

            Task.Run(() => Loop(tokenSource.Token))
                .ContinueWith(t => _logger.LogError(t.Exception, "loopTask failed with exception"), TaskContinuationOptions.OnlyOnFaulted);
            _logger.LogDebug("Loop started");
            return _browserPool.StartAsync(browsers);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (isDisposed)
                return;

            _logger.LogDebug("Stop started");
            tokenSource.Cancel();
            await _browserPool.StopAsync();
            StopBrowsers();
            _logger.LogDebug("All browsers been leved");
            isDisposed = true;
        }

        private async Task Loop(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                StopTaggedBrowsers();
                TagBrowsers();
                SendBeam();

                await StartBrowser(token);
                Thread.Sleep(200);
            }
        }

        private void StopTaggedBrowsers()
        {
            var browserToStop = browsers.FirstOrDefault(x => x.CanBeStopped && !x._isInWork);
            if (browserToStop != null)
            {
                browsers.Remove(browserToStop);
                browserToStop._driver.Quit();
            }
        }

        private void TagBrowsers()
        {
            if (!_poolSettings.Value.KeepAliveAtLeastOneBrowser
                    || browsers.Where(x => !x.CanBeStopped).Count() > 1)
            {
                var browserToStop = browsers
                    .FirstOrDefault(b => b.LastJobTime == browsers
                        .Where(x => !x._isInWork && !x.CanBeStopped)
                        .Where(x => _dateTimeProvider.UtcNow - x.LastJobTime > x._maxIdleTime)
                        .NullIfEmpty()
                        ?.Min(x => x.LastJobTime));

                if (browserToStop != null)
                    browserToStop.CanBeStopped = true;
            }
        }

        private void SendBeam()
        {
            if (!_poolSettings.Value.SendBeamPackages
                || _poolSettings.Value.BeamPackagesInterval <= TimeSpan.Zero)
                return;

            var browserToBeam = browsers
                .FirstOrDefault(b => b.LastBeamTime == browsers
                    .Where(x => !x._isInWork && !x.CanBeStopped)
                    .Where(x => _dateTimeProvider.UtcNow - x.LastBeamTime > _poolSettings.Value.BeamPackagesInterval)
                    .NullIfEmpty()
                    ?.Min(x => x.LastBeamTime));

            if (browserToBeam == null)
                return;

            var beam = new BeamCommand();
            _browserPool.DoJob(browserToBeam, new BeamCommand())
                .ContinueWith(t =>
                    _logger.LogWarning(
                        t.Exception,
                        "Error on beam {CommandId} on {BrowserId}",
                        beam.Id,
                        browserToBeam._id),
                    TaskContinuationOptions.OnlyOnFaulted);
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
                var browser = StartBrowserSafe((browsers.LastOrDefault()?._maxIdleTime ?? _poolSettings.Value.MaxIdleTime) + _poolSettings.Value.DeltaIdleTime);
                if (browser == null)
                    return;

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

        private BrowserWrapper StartBrowserSafe(TimeSpan maxIdleTime)
        {
            try
            {
                var driver = _factory.Create();
                return new BrowserWrapper(driver, maxIdleTime, _dateTimeProvider.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fail on create webDriver");
                return null;
            }
        }

        private void StopBrowsers()
        {
            browsers.ForEach(b =>
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
            browsers.Clear();
        }

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
            => Dispose(true);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
