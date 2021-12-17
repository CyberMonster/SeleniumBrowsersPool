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
    public class BrowserPool : IBrowserPoolAdvanced, IDisposable
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

        public Task DoJob(IBrowserCommand command)
        {
            if (!isNeedSaveState)
            {
                _logger.LogTrace("BrowserPool stopped");
                _stateProvider.SaveAction(command);
                return Task.CompletedTask;
            }
            else if ((_poolSettings.Value.QueueLimit - _actions.Count ?? 1) <= 0)
            {
                _logger.LogTrace("Queue overflowed");
                _stateProvider.SaveAction(command);
                return Task.CompletedTask;
            }

            _actions.Enqueue(command);
            return Task.CompletedTask;
        }

        async Task IBrowserPoolAdvanced.StartAsync(List<BrowserWrapper> browsers)
        {
            isNeedSaveState = true;

            _loopCancelTokenSource = new CancellationTokenSource();
            _browsers = new ConcurrentStack<BrowserWrapper>(browsers);
            _actions = new ConcurrentQueue<IBrowserCommand>(await _stateProvider.GetActions(_poolSettings.Value.QueueLimit));

            _ = Task.Run(() => Loop(_loopCancelTokenSource.Token))
                .ContinueWith(t => _logger.LogError(t.Exception, "loopTask failed with exception"), TaskContinuationOptions.OnlyOnFaulted);
        }

        public async Task LoadAdditionalActions(int take)
        {
            if (!isNeedSaveState)
                throw new InvalidOperationException($"{nameof(BrowserPool)} not started");

            var takeActionsCount = Math.Min(Math.Max(_poolSettings.Value.QueueLimit.Value - _actions.Count, 0), take);
            _logger.LogTrace("Load additional actions: {TakeActions}", takeActionsCount);

            var nextActions = _poolSettings.Value.QueueLimit.HasValue
                ? _stateProvider.GetActions(takeActionsCount)
                : _stateProvider.GetActions(null);
            foreach (var action in await nextActions)
            {
                if (_loopCancelTokenSource.Token.IsCancellationRequested)
                    await _stateProvider.SaveAction(action);
                else
                    _actions.Enqueue(action);
            }
        }

        Task IBrowserPoolAdvanced.RegisterBrowser(BrowserWrapper browser)
        {
            _browsers.Push(browser);
            return Task.CompletedTask;
        }

        public Task<int> GetQueueCount()
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

        private async Task DoJob(BrowserWrapper wrapper, IBrowserCommand command, CancellationToken deactivateToken)
        {
            using var _ = _logger.BeginScope("{JobType} {CommandId} on {BrowserId}", command.GetType().Name, command.Id, wrapper._id);
            _logger.LogDebug("Start job");

            if (!await SaveIfNotValidCommand(command))
            {
                _browsers.Push(wrapper);
                return;
            }

            if (wrapper._isInWork)
            {
                _logger.LogError(new InvalidOperationException("Browser wrapper is in inconsistent state. Field name _isInWork."), "Illegal state occurred");
                _actions.Enqueue(command);
                return;
            }

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
                try
                {
                    while (true)
                    {
                        if (localLoopCancelToken.IsCancellationRequested)
                            break;
                        else if (command.CancellationToken.IsCancellationRequested)
                            break;
                        else if (deactivateToken.IsCancellationRequested)
                        {
                            await _stateProvider.SaveAction(command);
                            break;
                        }

                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Can't save command. Command: {@Command}", command);
                }
            });
            try
            {
                await command.Execute(wrapper._driver, deactivateToken, _serviceProvider);
            }
            catch (Exception ex)
            {
                ++wrapper.Fails;
                _logger.LogError(ex, "Job fail");
                if (!deactivateToken.IsCancellationRequested)
                    _actions.Enqueue(command);
            }
            localLoopCancel.Cancel();
            wrapper._isInWork = false;
            await _stateProvider.NotifyActionProcessed(command);
            wrapper.LastJobTime = _dateTimeProvider.UtcNow;
            _logger.LogDebug("Finish job");

            if (wrapper.Fails < _poolSettings.Value.BrowserMaxFail)
                _browsers.Push(wrapper);
            else
                wrapper.CanBeStopped = true;
        }

        private async Task DoBeamJob(BrowserWrapper wrapper, BeamCommand command, CancellationToken deactivateToken)
        {
            using var _ = _logger.BeginScope("{JobType} {CommandId} on {BrowserId}", command.GetType().Name, command.Id, wrapper._id);
            _logger.LogDebug("Start job");

            if (wrapper._isInWork || deactivateToken.IsCancellationRequested)
                return;

            wrapper._isInWork = true;
            if (wrapper.CanBeStopped)
            {
                wrapper._isInWork = false;
                return;
            }

            try
            {
                await (command as IBrowserCommand).Execute(wrapper._driver, deactivateToken, _serviceProvider);
            }
            catch (Exception ex)
            {
                ++wrapper.Fails;
                _logger.LogError(ex, "Job fail");
            }
            wrapper._isInWork = false;
            wrapper.LastBeamTime = _dateTimeProvider.UtcNow;

            if (wrapper.Fails >= _poolSettings.Value.BrowserMaxFail)
                wrapper.CanBeStopped = true;
        }

        private async Task<bool> SaveIfNotValidCommand(IBrowserCommand command)
        {
            var result = true;
            if (command is BeamCommand)
            {
                _logger.LogCritical(new InvalidCommandException(command), "Can't handle command in current method");
                result = false;
            }
            else if (command.RunNumber >= _poolSettings.Value.CommandMaxRuns)
            {
                await _stateProvider.SaveProblemAction(command, CommandProblem.TooManyRuns);
                result = false;
            }
            else if (command.CancellationToken.IsCancellationRequested)
            {
                await _stateProvider.SaveProblemAction(command, CommandProblem.OperationCancelled);
                result = false;
            }

            return result;
        }

        Task IBrowserPoolAdvanced.DoJob(BrowserWrapper wrapper, BeamCommand command)
            => DoBeamJob(wrapper, command, _loopCancelTokenSource.Token);

        async Task IBrowserPoolAdvanced.StopAsync()
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
                (this as IBrowserPoolAdvanced).StopAsync().Wait();
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
