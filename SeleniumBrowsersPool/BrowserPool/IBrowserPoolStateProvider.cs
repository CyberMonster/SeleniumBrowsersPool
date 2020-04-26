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
    }
}
