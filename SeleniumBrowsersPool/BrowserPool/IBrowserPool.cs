using SeleniumBrowsersPool.BrowserPool.Commands;

using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public interface IBrowserPool
    {
        public Task DoJob(IBrowserCommand command);
    }
}
