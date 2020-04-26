using SeleniumBrowsersPool.BrowserPool.Commands;

namespace SeleniumBrowsersPool.BrowserPool
{
    public interface IBrowserPool
    {
        public void DoJob(IBrowserCommand command);
    }
}
