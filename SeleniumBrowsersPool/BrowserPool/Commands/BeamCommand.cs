using OpenQA.Selenium;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumBrowsersPool.BrowserPool.Commands
{
    internal class BeamCommand : BrowserCommandBase
    {
        public override Task Execute(WebDriver driver, CancellationToken cancellationToken, IServiceProvider serviceProvider, int runNumber)
        {
            cancellationToken.ThrowIfCancellationRequested();
            driver.GetScreenshot();
            return Task.CompletedTask;
        }
    }
}
