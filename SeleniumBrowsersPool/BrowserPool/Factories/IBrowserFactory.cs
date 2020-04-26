using OpenQA.Selenium.Remote;

namespace SeleniumBrowsersPool.BrowserPool.Factories
{
    public interface IBrowserFactory
    {
        public RemoteWebDriver Create();
    }
}
