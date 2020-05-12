using SeleniumBrowsersPool.BrowserPool.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public interface IBrowserPoolStateProvider
    {
        public Task SaveActions(IEnumerable<IBrowserCommand> commands);
        public Task SaveAction(IBrowserCommand command);
        public Task SaveProblemAction(IBrowserCommand command, CommandProblem problem);
        public Task<IEnumerable<IBrowserCommand>> GetActions();

        /// <summary>
        /// Return additional actions from storage.
        /// Needn't to implementation in common case of usage.
        /// You can use it from <see cref="IBrowserPoolAdvanced.LoadAdditionalActions"/> method to reduce the pool usage of RAM at startup.
        /// Current count of unhandled actions in pool queue you can see via <see cref="IBrowserPoolAdvanced.GetQueueLength"/>.
        /// </summary>
        public Task<IEnumerable<IBrowserCommand>> GetNextActions(int take);
    }
}
