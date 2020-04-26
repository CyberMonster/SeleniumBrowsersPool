using System;

namespace SeleniumBrowsersPool.Helpers
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
