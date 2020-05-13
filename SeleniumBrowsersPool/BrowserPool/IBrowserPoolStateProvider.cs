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
        /// <summary>
        /// Return data from storage.
        /// Null mean that queue haven't limitation. Need get all data from storage.
        /// </summary>
        /// <param name="take"></param>
        /// <returns></returns>
        public Task<IEnumerable<IBrowserCommand>> GetActions(int? take);
    }
}
