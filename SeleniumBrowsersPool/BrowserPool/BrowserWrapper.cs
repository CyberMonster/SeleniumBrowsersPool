using OpenQA.Selenium.Remote;
using System;

namespace SeleniumBrowsersPool.BrowserPool
{
    public class BrowserWrapper
    {
        public int Fails { get; set; }
        public bool CanBeStopped { get; internal set; }

        public readonly Guid _id = Guid.NewGuid();
        public readonly RemoteWebDriver _driver;
        public readonly TimeSpan _maxIdleTime;
        internal bool _onWork;

        public DateTime LastJobTime { get; internal set; }

        public BrowserWrapper(RemoteWebDriver driver, TimeSpan maxIdleTime, DateTime creationTime)
        {
            _driver = driver;
            _maxIdleTime = maxIdleTime;
            LastJobTime = creationTime;
        }
    }
}
