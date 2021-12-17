# Browsers pool

In the common case of selenium using you start the browser on each test. After that, you turn off the browser and do it again and again and again. But not in each case need restart the browser because an environment not change. In this case, and a case of production usage, you can use this pool for increase performance and utilization of resources.

![Build and push](https://github.com/CyberMonster/SeleniumBrowsersPool/workflows/Build%20and%20push/badge.svg?branch=master)

## Setup

Implement ```IBrowserFactory``` interface. It needed to create configured ```WebDriver```;
Implement ```IBrowserPoolStateProvider``` interface. It needed to save jobs data between stops;

Add in your configuration ```AddSeleniumBrowsersPool``` call:

```CS
services.AddSeleniumBrowsersPool<FirefoxFactory, BrowserPoolStateProvider>(Config);
```

Where ```FirefoxFactory``` is an implementation of the ```IBrowserFactory```
```BrowserPoolStateProvider``` is an implementation of the ```IBrowserPoolStateProvider```

### Advanced configuration

This package register the ```BrowserPoolSettings``` class to provide settings from ```IConfiguration```.
You can define the ```BrowserPoolSettings``` section in your config file.

```CSharp
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
```

```MaxDegreeOfParallel``` - set count of max active browsers;
```QueueLimit``` - limit of queue unhandled actions. Items that do not fit will be saved by ```IBrowserPoolStateProvider.SaveActions```;
```MaxIdleTime``` - idle time, after which browser will be closed;
```DeltaIdleTime``` - delta between browsers granted idle time;
```StartBrowsersOnRun``` - if true, browsers will be started on app start;
```KeepAliveAtLeastOneBrowser``` - if true, last active browser will be active always;
```CommandMaxRuns``` - count of job runs after which job be removed as failed;
```BrowserMaxFail``` - count of fail job on current browser instance, after which browser will be restarted;
```MaxQueueSizePerBrowser``` - if queue on browser less then this value, addition browser not be started;
```SendBeamPackages``` - if true, then if the browser in idle state, will be sent a package to prevent auto close by selenoid;
```BeamPackagesInterval``` - interval between beam packages.

### Connect to Selenoid

For connecting to [selenoid](https://aerokube.com/selenoid/latest/) just configure that. If you open [Ui](https://aerokube.com/selenoid-ui/latest/) of selenoid, on page Capabilities after choosing a current browser, you can copy code for connecting ```RemoteWebDriver```. After that create an implementation of the ```IBrowserFactory```:

```CSharp
public class FirefoxFactory : IBrowserFactory
{
    public WebDriver Create()
    {
        var capabilities = new DesiredCapabilities("firefox", "95.0.1", new Platform(PlatformType.Any));
        return new RemoteWebDriver(new Uri("http://localhost:4444/wd/hub"), capabilities);
    }
}
```

### Usage without selenoid

If you run browsers like this:

```CSharp
public class FirefoxFactory : IBrowserFactory
{
    public WebDriver Create()
    {
        return new FirefoxDriver();
    }
}
```

You can't stop the service correctly automatically. It's caused by the default behavior of drivers. On shutdown that permanently closing and you can't easily stop browsers. For correctly finish work you can get ```IBrowserPoolArbitrator``` via ```ServiceProvider``` and call ```StopAsync``` with the empty token.

```CSharp
public class AdminController : ControllerBase
{
    private readonly IBrowserPoolArbitrator _browserPoolArbitrator;

    public AdminController(IBrowserPoolArbitrator browserPoolArbitrator)
        => _browserPoolArbitrator = browserPoolArbitrator;

    [HttpGet("stop")]
    public Task Stop()
        => _browserPoolArbitrator.StopAsync(new System.Threading.CancellationToken());
}
```

You needn't do that if you use selenoid. With selenoid service stop correctly.
