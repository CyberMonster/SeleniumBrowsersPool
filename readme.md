# Browsers pool

In the common case of selenium using you start the browser on each test. After that, you turn off the browser and do it again and again and again. But not in each case need restart the browser because an environment not change. In this case, and a case of production usage, you can use this pool for increase performance and utilization of resources.

## Setup

Implement ```IBrowserFactory``` interface. It needed to create configurated ```RemoteWebDriver```;
Implement ```IBrowserPoolStateProvider``` interface. It needed to save jobs data between stops;

Add in your configuration ```AddSeleniumBrowsersPool``` call:

```CS
services.AddSeleniumBrowsersPool<FirefoxFactory, BrowserPoolStateProvider>(Config);
```

Where ```FirefoxFactory``` is an implementation of the ```IBrowserFactory```
```BrowserPoolStateProvider``` is an implementation of the ```IBrowserPoolStateProvider```

### Connect to Selenoid

For connecting to [selenoid](https://aerokube.com/selenoid/latest/) just configure that. If you open [Ui](https://aerokube.com/selenoid-ui/latest/) of selenoid, on page Capabilities after choosing a current browser, you can copy code for connecting ```RemoteWebDriver```. After that create an implementation of the ```IBrowserFactory```:

```CSharp
public class FirefoxFactory : IBrowserFactory
{
    public RemoteWebDriver Create()
    {
        var capabilities = new DesiredCapabilities("firefox", "75.0", new Platform(PlatformType.Any));
        return new RemoteWebDriver(new Uri("http://localhost:4444/wd/hub"), capabilities);
    }
}
```

### Usage without selenoid

If you run browsers like this:

```CSharp
public class FirefoxFactory : IBrowserFactory
{
    public RemoteWebDriver Create()
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
