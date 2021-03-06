﻿using System;

namespace SeleniumBrowsersPool
{
    public class BrowserPoolSettings
    {
        public int MaxDegreeOfParallel { get; set; } = 1;
        public int? QueueLimit { get; set; } = null;
        public TimeSpan MaxIdleTime { get; set; } = TimeSpan.FromMinutes(10);
        public TimeSpan DeltaIdleTime { get; set; } = TimeSpan.Zero;
        public bool StartBrowsersOnRun { get; set; } = false;
        public bool KeepAliveAtLeastOneBrowser { get; set; } = true;
        public int CommandMaxRuns { get; set; } = 3;
        public int BrowserMaxFail { get; set; } = 3;
        public int MaxQueueSizePerBrowser { get; set; } = 3;
        public bool SendBeamPackages { get; set; } = false;
        public TimeSpan BeamPackagesInterval { get; set; } = TimeSpan.FromMinutes(1);
    }
}
