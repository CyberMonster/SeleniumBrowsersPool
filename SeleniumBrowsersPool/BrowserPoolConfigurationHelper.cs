using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeleniumBrowsersPool.BrowserPool;
using SeleniumBrowsersPool.BrowserPool.Factories;
using SeleniumBrowsersPool.Helpers;

namespace SeleniumBrowsersPool
{
    public static class BrowserPoolConfigurationHelper
    {
        public static IServiceCollection AddBrowserFactory<TImplementation>(this IServiceCollection services)
            where TImplementation : class, IBrowserFactory
            => services.AddSingleton<IBrowserFactory, TImplementation>();

        public static IServiceCollection AddStateProvider<TImplementation>(this IServiceCollection services)
            where TImplementation : class, IBrowserPoolStateProvider
            => services.AddSingleton<IBrowserPoolStateProvider, TImplementation>();

        public static IServiceCollection AddBrowserPool(this IServiceCollection services, IConfiguration cfg)
            => services.AddOptions()
                .Configure<BrowserPoolSettings>(cfg.GetSection(typeof(BrowserPoolSettings).Name))
                .AddSingleton<IBrowserPoolArbitrator, BrowserPoolArbitrator>()
                .AddSingleton<IHostedService>(f => f.GetService<IBrowserPoolArbitrator>())
                .AddSingleton<IBrowserPool, BrowserPool.BrowserPool>()
                .AddSingleton(sp => sp.GetService<IBrowserPool>() as IBrowserPoolInternal)
                .AddSingleton<IDateTimeProvider, DateTimeProvider>();

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider>(this IServiceCollection services, IConfiguration cfg)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            => services.AddBrowserPool(cfg)
                .AddBrowserFactory<TBrowserFactory>()
                .AddStateProvider<TBrowserPoolStateProvider>();
    }
}
