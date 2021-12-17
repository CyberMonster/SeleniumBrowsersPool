using OpenQA.Selenium;

namespace SeleniumBrowsersPool.BrowserPool.Factories
{
    public interface IBrowserFactory
    {
        public WebDriver Create();
    }
}
