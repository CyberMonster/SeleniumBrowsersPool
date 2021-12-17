using OpenQA.Selenium;

using System;

namespace SeleniumBrowsersPool.BrowserPool
{
    public class BrowserWrapper
    {
        public int Fails { get; set; }
        public bool CanBeStopped { get; internal set; }

        public readonly Guid _id = Guid.NewGuid();
        public readonly WebDriver _driver;
        public readonly TimeSpan _maxIdleTime;
        internal bool _isInWork;

        public DateTime LastJobTime { get; internal set; }
        public DateTime LastBeamTime { get; internal set; }

        public BrowserWrapper(WebDriver driver, TimeSpan maxIdleTime, DateTime creationTime)
        {
            _driver = driver;
            _maxIdleTime = maxIdleTime;
            LastJobTime = creationTime;
            LastBeamTime = creationTime;
        }
    }
}
