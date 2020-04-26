using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool
{
    public interface IBrowserPoolArbitrator : IHostedService
    {
        public Task StopAsync(CancellationToken cancellationToken);
    }
}
