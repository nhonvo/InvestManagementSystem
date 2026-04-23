using Hangfire;
using Hangfire.PostgreSql;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure;
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
using Serilog.Events;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var bootstrapSettings = configuration.Get<WorkerSettings>()
    ?? throw new InvalidOperationException("WorkerSettings configuration is missing.");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(bootstrapSettings.Seq.ServerUrl)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddSerilog();

    var settings = builder.Configuration.Get<WorkerSettings>() ?? bootstrapSettings;
    builder.Services.AddSingleton(settings);
    builder.Services.AddSingleton<InventoryAlert.Domain.Configuration.AppSettings>(settings);

    builder.Services.AddInfrastructure(settings);
    builder.Services.SetupHealthCheck(settings);

    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(settings.Database.DefaultConnection)));

    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 4;
        opts.ServerName = "finance-worker";
    });

    // Scheduled Jobs
    builder.Services.AddScoped<SyncPricesJob>();
    builder.Services.AddScoped<SyncMetricsJob>();
    builder.Services.AddScoped<SyncEarningsJob>();
    builder.Services.AddScoped<SyncRecommendationsJob>();
    builder.Services.AddScoped<SyncInsidersJob>();
    builder.Services.AddScoped<CompanyNewsJob>();
    builder.Services.AddScoped<CleanupPriceHistoryJob>();
    builder.Services.AddScoped<IProcessQueueJob, ProcessQueueJob>();

    // Integration Event Handlers
    builder.Services.AddScoped<MarketPriceAlertHandler>();
    builder.Services.AddScoped<CompanyNewsAlertHandler>();
    builder.Services.AddScoped<SyncMarketNewsHandler>();
    builder.Services.AddScoped<LowHoldingsHandler>();

    builder.Services.AddHostedService<JobSchedulerService>();
    builder.Services.AddHostedService<SqsListenerService>();

    builder.Services.AddScoped<IRawDefaultHandler, DefaultHandler>();
    builder.Services.AddScoped<InventoryAlert.Worker.Interfaces.IIntegrationMessageRouter, IntegrationMessageRouter>();

    builder.Services.AddScoped<ISqsHelper, SqsHelper>();

    // Notification Delivery
    builder.Services.AddScoped<IAlertNotifier, NotificationAlertNotifier>();

    var app = builder.Build();

    // Allow remote dashboard access in Dev/Docker
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
