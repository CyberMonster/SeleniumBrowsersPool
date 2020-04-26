using System;

namespace SeleniumBrowsersPool.Helpers
{
    public interface IDateTimeProvider
    {
        public DateTime UtcNow { get; }
    }
}
