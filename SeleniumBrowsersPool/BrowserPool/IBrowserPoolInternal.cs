using SeleniumBrowsersPool.BrowserPool.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public interface IBrowserPoolAdvanced : IBrowserPool
    {
        public Task<int> GetQueueCount();
        public Task LoadAdditionalActions(int take);
        internal Task StopAsync();
        internal Task StartAsync(List<BrowserWrapper> browsers);
        internal Task RegisterBrowser(BrowserWrapper browser);
        internal Task DoJob(BrowserWrapper wrapper, BeamCommand command);
    }
}
