using OpenQA.Selenium.Remote;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool.Commands
{
    public abstract class BrowserCommandBase : IBrowserCommand
    {
        public CancellationToken CancellationToken { get; set; }
        internal int RunNumber { get; private set; }
        int IBrowserCommand.RunNumber => RunNumber;
        public Guid Id { get; private set; }

        public BrowserCommandBase()
            => Id = Guid.NewGuid();

        Task IBrowserCommand.Execute(RemoteWebDriver driver, CancellationToken cancellationToken, IServiceProvider serviceProvider)
            => Execute(driver, cancellationToken, serviceProvider, RunNumber++);

        public abstract Task Execute(RemoteWebDriver driver, CancellationToken cancellationToken, IServiceProvider serviceProvider, int runNumber);
    }
}
