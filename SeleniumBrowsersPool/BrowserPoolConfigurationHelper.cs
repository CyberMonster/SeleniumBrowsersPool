using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeleniumBrowsersPool.BrowserPool;
using SeleniumBrowsersPool.BrowserPool.Factories;
using SeleniumBrowsersPool.Helpers;
using System;

namespace SeleniumBrowsersPool
{
    public static class BrowserPoolConfigurationHelper
    {
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
                .AddSingleton<IBrowserFactory, TBrowserFactory>()
                .AddSingleton<IBrowserPoolStateProvider, TBrowserPoolStateProvider>();

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider>(this IServiceCollection services, IConfiguration cfg, TBrowserFactory browserFactory, TBrowserPoolStateProvider stateProvider)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            => services.AddBrowserPool(cfg)
                .AddSingleton(browserFactory)
                .AddSingleton(stateProvider);

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider>(this IServiceCollection services, IConfiguration cfg, Func<IServiceProvider, TBrowserFactory> browserFactory, Func<IServiceProvider, TBrowserPoolStateProvider> stateProvider)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            => services.AddBrowserPool(cfg)
                .AddSingleton(browserFactory)
                .AddSingleton(stateProvider);

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider>(this IServiceCollection services, IConfiguration cfg, TBrowserFactory browserFactory, Func<IServiceProvider, TBrowserPoolStateProvider> stateProvider)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            => services.AddBrowserPool(cfg)
                .AddSingleton(browserFactory)
                .AddSingleton(stateProvider);

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider>(this IServiceCollection services, IConfiguration cfg, Func<IServiceProvider, TBrowserFactory> browserFactory, TBrowserPoolStateProvider stateProvider)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            => services.AddBrowserPool(cfg)
                .AddSingleton(browserFactory)
                .AddSingleton(stateProvider);
    }
}
