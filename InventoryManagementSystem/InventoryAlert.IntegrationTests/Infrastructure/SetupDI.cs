using InventoryAlert.Api.Configuration;
using InventoryAlert.Api.ServiceExtensions;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Infrastructure;
using InventoryAlert.Infrastructure.Hubs;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.IntegrationEvents.Routing;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.ScheduledJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace InventoryAlert.IntegrationTests.Infrastructure;

public static class SetupDI
{
    public static IServiceProvider BuildApiServiceProvider(IConfiguration config, TestLoggerProvider loggerProvider, Action<IServiceCollection>? overrides = null)
    {
        var services = new ServiceCollection();
        var settings = config.Get<ApiSettings>() ?? new ApiSettings();

        // ── Standard API Registrations ────────────────────────────────────
        services.AddSingleton(config);
        services.AddSingleton(settings);
        services.AddSingleton<AppSettings>(settings);
        services.AddWebApiInfrastructure(settings);

        // ── SignalR Mock (Avoids needing Redis backplane for logic tests) ──
        services.AddSingleton(Mock.Of<IHubContext<NotificationHub, INotificationHub>>());

        // ── Logging Override ──────────────────────────────────────────────
        services.AddSingleton<ILoggerProvider>(loggerProvider);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        overrides?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public static IServiceProvider BuildWorkerServiceProvider(IConfiguration config, TestLoggerProvider loggerProvider, Action<IServiceCollection>? overrides = null)
    {
        var services = new ServiceCollection();
        var settings = config.Get<WorkerSettings>() ?? new WorkerSettings();

        // ── Standard Worker Registrations ─────────────────────────────────
        services.AddSingleton(config);
        services.AddSingleton(settings);
        services.AddSingleton<AppSettings>(settings);
        services.AddInfrastructure(settings);

        // ── SignalR Mock (Avoids needing Redis backplane for logic tests) ──
        services.AddSingleton(Mock.Of<IHubContext<NotificationHub, INotificationHub>>());

        // Jobs
        services.AddScoped<SyncPricesJob>();
        services.AddScoped<SyncMetricsJob>();
        services.AddScoped<SyncEarningsJob>();
        services.AddScoped<SyncRecommendationsJob>();
        services.AddScoped<SyncInsidersJob>();
        services.AddScoped<NewsSyncJob>();
        services.AddScoped<CleanupPriceHistoryJob>();
        services.AddScoped<IProcessQueueJob, ProcessQueueJob>();

        // Handlers
        services.AddScoped<MarketPriceAlertHandler>();
        services.AddScoped<LowHoldingsHandler>();
        services.AddScoped<IRawDefaultHandler, DefaultHandler>();
        services.AddScoped<IIntegrationMessageRouter, IntegrationMessageRouter>();

        // ── Logging Override ──────────────────────────────────────────────
        services.AddSingleton<ILoggerProvider>(loggerProvider);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(loggerProvider);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        overrides?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
