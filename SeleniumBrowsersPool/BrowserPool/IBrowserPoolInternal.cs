using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public interface IBrowserPoolInternal
    {
        public Task StopAsync();
        public Task StartAsync(List<BrowserWrapper> browsers);
        public Task RegisterBrowser(BrowserWrapper browser);
        public Task<int> GetQueueLength();
    }
}
