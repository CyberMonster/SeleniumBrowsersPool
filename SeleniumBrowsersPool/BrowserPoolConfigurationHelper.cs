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
        public static IServiceCollection AddBrowserPool<TDateTimeProvider>(this IServiceCollection services, IConfiguration cfg)
            where TDateTimeProvider : class, IDateTimeProvider
            => services.AddOptions()
                .Configure<BrowserPoolSettings>(cfg.GetSection(typeof(BrowserPoolSettings).Name))
                .AddSingleton<IBrowserPoolArbitrator, BrowserPoolArbitrator>()
                .AddSingleton<IHostedService>(f => f.GetService<IBrowserPoolArbitrator>())
                .AddSingleton<IBrowserPoolAdvanced, BrowserPool.BrowserPool>()
                .AddSingleton(sp => sp.GetService<IBrowserPoolAdvanced>() as IBrowserPool)
                .AddSingleton<IDateTimeProvider, TDateTimeProvider>();

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider, TDateTimeProvider>(this IServiceCollection services, IConfiguration cfg)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            where TDateTimeProvider : class, IDateTimeProvider
            => services.AddBrowserPool<TDateTimeProvider>(cfg)
                .AddSingleton<IBrowserFactory, TBrowserFactory>()
                .AddSingleton<IBrowserPoolStateProvider, TBrowserPoolStateProvider>();

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TBrowserPoolStateProvider, TDateTimeProvider>(this IServiceCollection services, IConfiguration cfg, TBrowserFactory browserFactory, TBrowserPoolStateProvider stateProvider)
            where TBrowserFactory : class, IBrowserFactory
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            where TDateTimeProvider : class, IDateTimeProvider
            => services.AddBrowserPool<TDateTimeProvider>(cfg)
                .AddSingleton<IBrowserFactory>(browserFactory)
                .AddSingleton<IBrowserPoolStateProvider>(stateProvider);

        public static IServiceCollection AddSeleniumBrowsersPool<TDateTimeProvider>(this IServiceCollection services, IConfiguration cfg, Func<IServiceProvider, IBrowserFactory> browserFactory, Func<IServiceProvider, IBrowserPoolStateProvider> stateProvider)
            where TDateTimeProvider : class, IDateTimeProvider
            => services.AddBrowserPool<TDateTimeProvider>(cfg)
                .AddSingleton(browserFactory)
                .AddSingleton(stateProvider);

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserFactory, TDateTimeProvider>(this IServiceCollection services, IConfiguration cfg, Func<IServiceProvider, IBrowserPoolStateProvider> stateProvider)
            where TBrowserFactory : class, IBrowserFactory
            where TDateTimeProvider : class, IDateTimeProvider
            => services.AddBrowserPool<TDateTimeProvider>(cfg)
                .AddSingleton<IBrowserFactory, TBrowserFactory>()
                .AddSingleton(stateProvider);

        public static IServiceCollection AddSeleniumBrowsersPool<TBrowserPoolStateProvider, TDateTimeProvider>(this IServiceCollection services, IConfiguration cfg, Func<IServiceProvider, IBrowserFactory> browserFactory)
            where TBrowserPoolStateProvider : class, IBrowserPoolStateProvider
            where TDateTimeProvider : class, IDateTimeProvider
            => services.AddBrowserPool<TDateTimeProvider>(cfg)
                .AddSingleton(browserFactory)
                .AddSingleton<IBrowserPoolStateProvider, TBrowserPoolStateProvider>();
    }
}
