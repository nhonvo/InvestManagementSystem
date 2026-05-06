using Hangfire;
using Hangfire.PostgreSql;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure;
using InventoryAlert.Infrastructure.Utilities;
using InventoryAlert.Worker;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Hosting;
using InventoryAlert.Worker.IntegrationEvents.Handlers;
using InventoryAlert.Worker.IntegrationEvents.Routing;
using InventoryAlert.Worker.Interfaces;
using InventoryAlert.Worker.ScheduledJobs;
using InventoryAlert.Worker.Utilities;
using InventoryAlert.Worker.Extensions;
using Serilog;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.Get<WorkerSettings>()
    ?? throw new InvalidOperationException("WorkerSettings configuration is missing.");

// ─── Serilog bootstrap ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ApplyBaseConfiguration(settings, "InventoryAlert.Worker")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ApplyBaseConfiguration(settings, "InventoryAlert.Worker")
            .ReadFrom.Services(services)
            .Enrich.With(services.GetRequiredService<CorrelationIdEnricher>());
    });

    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton<InventoryAlert.Domain.Configuration.AppSettings>(settings);
    builder.Services.AddCorrelationEnricher();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddInfrastructure(settings);
    builder.Services.SetupHealthCheck(settings);

    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(settings.Database.DefaultConnection)));

    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 4;
        opts.ServerName = "inventory-worker";
    });

    // ─── SignalR with Redis Backplane ─────────────────────────────────────────
    builder.Services.AddSignalR()
        .AddStackExchangeRedis(settings.Redis.ConnectionString, options => {
            options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("InventoryAlert_SignalR");
        });

    // Scheduled Jobs
    builder.Services.AddScoped<SyncPricesJob>();
    builder.Services.AddScoped<SyncMetricsJob>();
    builder.Services.AddScoped<SyncEarningsJob>();
    builder.Services.AddScoped<SyncRecommendationsJob>();
    builder.Services.AddScoped<SyncInsidersJob>();
    builder.Services.AddScoped<NewsSyncJob>();
    builder.Services.AddScoped<CleanupPriceHistoryJob>();
    builder.Services.AddScoped<IProcessQueueJob, ProcessQueueJob>();

    // Integration Event Handlers
    builder.Services.AddScoped<MarketPriceAlertHandler>();
    builder.Services.AddScoped<LowHoldingsHandler>();
    builder.Services.AddScoped<IRawDefaultHandler, DefaultHandler>();
    builder.Services.AddScoped<InventoryAlert.Worker.Interfaces.IIntegrationMessageRouter, IntegrationMessageRouter>();
    builder.Services.AddScoped<ISqsHelper, SqsHelper>();

    builder.Services.AddHostedService<JobSchedulerService>();
    builder.Services.AddHostedService<SqsListenerService>();

    var app = builder.Build();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new DevDashboardAuthorizationFilter() }
    });

    app.ConfigureHealthCheck();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
