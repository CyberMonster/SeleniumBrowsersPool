using OpenQA.Selenium.Remote;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool.Commands
{
    /// <summary>
    /// Not for impl in derived class.
    /// Use <see cref= "BrowserCommandBase"/>
    /// </summary>
    public interface IBrowserCommand
    {
        public CancellationToken CancellationToken { get; set; }
        public Guid Id { get; }
        internal int RunNumber { get; }

        /// <summary>
        /// Exec command on browser.
        /// Not for impl in derived class.
        /// </summary>
        /// <param name="driver"> current driver that run command </param>
        /// <param name="cancellationToken"> if true that command was be cancelled and command enqueue back </param>
        internal Task Execute(RemoteWebDriver driver, CancellationToken cancellationToken, IServiceProvider serviceProvider);

        /// <summary>
        /// Exec command on browser. Only for impl in derived class.
        /// Call via <see cref="Execute"/>
        /// </summary>
        /// <param name="driver"> current driver that run command </param>
        /// <param name="cancellationToken"> if true that command was be cancelled and command enqueue back </param>
        public Task Execute(RemoteWebDriver driver, CancellationToken cancellationToken, IServiceProvider serviceProvider, int runNumber);
    }
}
