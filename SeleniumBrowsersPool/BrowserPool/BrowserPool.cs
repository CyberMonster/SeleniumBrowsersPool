using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeleniumBrowsersPool.BrowserPool.Commands;
using SeleniumBrowsersPool.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public class BrowserPool : IBrowserPool, IBrowserPoolInternal, IDisposable
    {
        private ConcurrentQueue<IBrowserCommand> _actions;
        private ConcurrentStack<BrowserWrapper> _browsers;
        private CancellationTokenSource _loopCancelTokenSource;

        private readonly IServiceProvider _serviceProvider;
        private readonly IBrowserPoolStateProvider _stateProvider;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOptions<BrowserPoolSettings> _poolSettings;
        private readonly ILogger<BrowserPool> _logger;

        public BrowserPool(IServiceProvider serviceProvider,
                           IBrowserPoolStateProvider stateProvider,
                           IDateTimeProvider dateTimeProvider,
                           IOptions<BrowserPoolSettings> poolSettings,
                           ILogger<BrowserPool> logger)
        {
            _serviceProvider = serviceProvider;
            _stateProvider = stateProvider;
            _dateTimeProvider = dateTimeProvider;
            _poolSettings = poolSettings;
            _logger = logger;
        }

        public void DoJob(IBrowserCommand command)
            => _actions.Enqueue(command);

        public async Task StartAsync(List<BrowserWrapper> browsers)
        {
            _loopCancelTokenSource = new CancellationTokenSource();
            _browsers = new ConcurrentStack<BrowserWrapper>(browsers);
            _actions = new ConcurrentQueue<IBrowserCommand>(await _stateProvider.GetActions());

            isNeedSaveState = true;
            Task.Run(() => Loop(_loopCancelTokenSource.Token))
                .ContinueWith(t => _logger.LogError(t.Exception, "loopTask failed with exception"), TaskContinuationOptions.OnlyOnFaulted);
            return;
        }

        public Task RegisterBrowser(BrowserWrapper browser)
        {
            _browsers.Push(browser);
            return Task.CompletedTask;
        }

        public Task<int> GetQueueLength()
            => Task.FromResult(_actions?.Count ?? 0);

        private void Loop(CancellationToken token)
        {
            _logger.LogInformation("Start loop");
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                if (_actions.Count > 0 && _browsers.Count > 0)
                    if (_browsers.TryPop(out var wrapper) && _actions.TryDequeue(out var action))
                        Task.Run(() => DoJob(wrapper, action, token));

                Thread.Sleep(200);
            }
        }

        public async Task DoJob(BrowserWrapper wrapper, IBrowserCommand command, CancellationToken deactivateToken)
        {
            using var _ = _logger.BeginScope("{JobType} {CommandId} on {BrowserId}", command.GetType().Name, command.Id, wrapper._id);
            _logger.LogDebug("Start job");

            if (!await SaveIfNotValidCommand(command))
                return;

            var isBeamCommand = command is BeamCommand;
            if (wrapper._isInWork)
                if (isBeamCommand)
                    return;
                else
                    throw new InvalidOperationException("Browser wrapper is in inconsistent state. Field name _isInWork.");

            wrapper._isInWork = true;
            if (wrapper.CanBeStopped)
            {
                _logger.LogDebug("Current browser will be stopped. Reenqueue action.");
                _actions.Enqueue(command);
                wrapper._isInWork = false;
                return;
            }

            _logger.LogDebug("Validation completed");

            var localLoopCancel = new CancellationTokenSource();
            var localLoopCancelToken = localLoopCancel.Token;
            var t = Task.Run(async () =>
            {
                while (true)
                {
                    if (localLoopCancelToken.IsCancellationRequested)
                        break;
                    else if (deactivateToken.IsCancellationRequested)
                    {
                        await _stateProvider.SaveAction(command);
                        break;
                    }

                    Thread.Sleep(200);
                }
            });
            try
            {
                await command.Execute(wrapper._driver, deactivateToken, _serviceProvider);
            }
            catch (Exception ex)
            {
                ++wrapper.Fails;
                _logger.LogError(ex, "Job fail", command.GetType().Name, wrapper._id);
                if (!isBeamCommand)
                    _actions.Enqueue(command);
            }
            localLoopCancel.Cancel();
            wrapper._isInWork = false;
            _logger.LogDebug("Finish job", command.GetType().Name, wrapper._id);
            if (isBeamCommand)
                wrapper.LastBeamTime = _dateTimeProvider.UtcNow;
            else
                wrapper.LastJobTime = _dateTimeProvider.UtcNow;

            if (wrapper.Fails < _poolSettings.Value.BrowserMaxFail)
                _browsers.Push(wrapper);
            else
                wrapper.CanBeStopped = true;
        }

        private async Task<bool> SaveIfNotValidCommand(IBrowserCommand command)
        {
            if (command is BeamCommand)
            {
                return true;
            }
            else if (command.RunNumber > _poolSettings.Value.CommandMaxRuns)
            {
                await _stateProvider.SaveProblemAction(command, CommandProblem.TooManyRuns);
                return false;
            }
            else if (command.CancellationToken.IsCancellationRequested)
            {
                await _stateProvider.SaveProblemAction(command, CommandProblem.OperationCancelled);
                return true;
            }

            return true;
        }

        Task IBrowserPoolInternal.DoJob(BrowserWrapper wrapper, BeamCommand command)
            => DoJob(wrapper, command, new CancellationToken());

        public async Task StopAsync()
        {
            _logger.LogInformation("Begin stop");
            _loopCancelTokenSource.Cancel();

            await SaveState();
            isNeedSaveState = false;
            _logger.LogInformation("Browsers pool stopped");
        }

        private async Task SaveState()
        {
            if (_actions != null && _actions.Count > 0)
                await _stateProvider.SaveActions(_actions);

            _actions = null;
        }

        #region IDisposable Support
        private bool isNeedSaveState = true;

        protected void Dispose(bool _)
        {
            if (isNeedSaveState)
                StopAsync().Wait();
            else
                _logger.LogInformation("State already saved");
        }

        ~BrowserPool()
            => Dispose(true);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
