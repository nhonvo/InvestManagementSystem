using Amazon.DynamoDBv2;
using Amazon.SQS;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Persistence.Repositories;
using InventoryAlert.Worker.Application;
using InventoryAlert.Worker.Application.IntegrationHandlers;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Configuration;
using InventoryAlert.Worker.Filters;
using InventoryAlert.Worker.Infrastructure.BackgroundServices;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using InventoryAlert.Worker.Infrastructure.Jobs;
using InventoryAlert.Worker.Infrastructure.MessageConsumers;
using InventoryAlert.Worker.Infrastructure.Messaging;
using InventoryAlert.Worker.Interfaces;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

// ─── Early Configuration Binding for Bootstrap ───────────────────────────────
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
    .AddEnvironmentVariables()
    .Build();

var bootstrapSettings = configuration.Get<WorkerSettings>()
    ?? throw new InvalidOperationException("WorkerSettings configuration is missing.");

// ─── Serilog bootstrap (identical pattern to the Api) ─────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(bootstrapSettings.Seq.ServerUrl)
    .WriteTo.File("logs/worker-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────────
    builder.Services.AddSerilog();

    // ── Configuration ─────────────────────────────────────────────────────────
    var settings = builder.Configuration.Get<WorkerSettings>() ?? bootstrapSettings;

    builder.Services.AddSingleton(settings);

    // ── EF Core (shared PostgreSQL schema with API) ───────────────────────────
    builder.Services.AddDbContext<InventoryDbContext>(opts =>
    {
        opts.UseNpgsql(settings.Database.DefaultConnection)
            .LogTo(Log.Information, new[] { DbLoggerCategory.Database.Command.Name }, Microsoft.Extensions.Logging.LogLevel.Information);
    });

    // ── Redis Distributed Cache ───────────────────────────────────────────────
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(settings.Redis.ConnectionString));

    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = settings.Redis.ConnectionString;
        opts.InstanceName = "inventoryalert:";
    });

    // ── AWS SQS & DynamoDB (Moto in dev) ──────────────────────────────────────
    builder.Services.AddSingleton<IAmazonSQS>(_ =>
    {
        var config = new AmazonSQSConfig { ServiceURL = settings.Aws.EndpointUrl };
        return new AmazonSQSClient(config);
    });

    builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    {
        var config = new AmazonDynamoDBConfig { ServiceURL = settings.Aws.EndpointUrl };
        return new AmazonDynamoDBClient(config);
    });

    // ── External API Client (Finnhub) ─────────────────────────────────────────
    builder.Services.AddSingleton(_ =>
        new RestClient(new RestClientOptions(settings.Finnhub.ApiBaseUrl)
        {
            ThrowOnAnyError = false
        }));

    builder.Services.AddSingleton<IFinnhubClient, FinnhubClient>();

    // ── Hangfire ──────────────────────────────────────────────────────────────
    builder.Services.AddHangfire(config =>
        config
            .UseFilter(new HangfireJobLoggingFilter())   // Global job error filter
            .UsePostgreSqlStorage(opts =>
                opts.UseNpgsqlConnection(settings.Database.DefaultConnection)));

    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 2;
        opts.ServerName = "inventory-worker";
    });

    // ── Shared Messaging Infrastructure ───────────────────────────────────────
    builder.Services.AddSingleton<ISqsHelper, SqsHelper>();
    builder.Services.AddScoped<ISqsDispatcher, SqsDispatcher>();
    builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();

    // ── Application Integration Handlers ──────────────────────────────────────
    builder.Services.AddScoped<IPriceAlertHandler, PriceAlertHandler>();
    builder.Services.AddScoped<INewsHandler, NewsHandler>();
    builder.Services.AddScoped<IStockLowHandler, StockLowHandler>();
    builder.Services.AddScoped<IRawDefaultHandler, DefaultHandler>();
    builder.Services.AddScoped<UnknownEventHandler>();

    // New Event Handlers
    builder.Services.AddScoped<IEventHandler<InventoryAlert.Contracts.Events.Payloads.PriceUpdatePayload>, PriceUpdateHandler>();
    builder.Services.AddScoped<IEventHandler<InventoryAlert.Contracts.Events.Payloads.MarketNewsPayload>, MarketNewsHandler>();
    builder.Services.AddScoped<IEventHandler<InventoryAlert.Contracts.Events.Payloads.EarningsPayload>, EarningsHandler>();
    builder.Services.AddScoped<IEventHandler<InventoryAlert.Contracts.Events.Payloads.RecommendationUpdatedPayload>, RecommendationHandler>();
    builder.Services.AddScoped<IEventHandler<InventoryAlert.Contracts.Events.Payloads.SymbolAddedPayload>, SymbolAddedHandler>();
    builder.Services.AddScoped<IEventHandler<InventoryAlert.Contracts.Events.Payloads.AlertRulePayload>, AlertRuleHandler>();

    // Telegram
    builder.Services.AddScoped<InventoryAlert.Worker.Application.Telegram.TelegramBotService>();
    builder.Services.AddScoped<InventoryAlert.Worker.Application.Telegram.PriceCommandHandler>();
    builder.Services.AddScoped<InventoryAlert.Worker.Application.Telegram.NewsCommandHandler>();
    builder.Services.AddScoped<InventoryAlert.Worker.Application.Telegram.RecommendCommandHandler>();
    builder.Services.AddScoped<InventoryAlert.Worker.Application.Telegram.EarningsCommandHandler>();

    // ── Repositories (Worker Specific & Shared) ───────────────────────────────
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
    builder.Services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
    builder.Services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    builder.Services.AddScoped<INewsDynamoRepository, NewsDynamoRepository>();
    builder.Services.AddScoped<IPriceHistoryDynamoRepository, PriceHistoryDynamoRepository>();
    builder.Services.AddScoped<IMarketNewsDynamoRepository, MarketNewsDynamoRepository>();
    builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
    builder.Services.AddScoped<IEarningsDynamoRepository, EarningsDynamoRepository>();

    // ── Hangfire Jobs (Scoped) ────────────────────────────────────────────────
    builder.Services.AddScoped<PollSqsJob>();
    builder.Services.AddScoped<SyncPricesJob>();
    builder.Services.AddScoped<NewsCheckJob>();

    // New Hangfire Jobs
    builder.Services.AddScoped<MarketNewsJob>();
    builder.Services.AddScoped<RecommendationsJob>();
    builder.Services.AddScoped<EarningsJob>();
    builder.Services.AddScoped<ProfileSyncJob>();
    builder.Services.AddScoped<SymbolCrawlJob>();
    builder.Services.AddScoped<MarketStatusJob>();
    builder.Services.AddScoped<EarningsCalendarJob>();

    // ── Background Task Queue (In-Memory) ─────────────────────────────────────
    builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx =>
    {
        var capacity = ctx.GetRequiredService<WorkerSettings>().SqsPolling.QueueCapacity;
        return new BackgroundTaskQueue(capacity);
    });

    // ── Scheduled & Background Services ───────────────────────────────────────
    builder.Services.AddHostedService<JobSchedulerService>();
    builder.Services.AddHostedService<QueuedHostedService>();

    // ── SQS Polling Strategy (Conditional Registration) ───────────────────────

    builder.Services.AddScoped<IProcessQueueJob, ProcessQueueJob>();
    builder.Services.AddHostedService<NativeSqsWorker>();
    // PollSqsJob (Path A) is scheduled via Hangfire — NOT registered as a HostedService.
    // NativeSqsWorker (Path B) is running as a HostedService above.


    var app = builder.Build();

    // Hangfire UI — restricted to localhost in non-dev environments.
    // In production, place behind an authenticated reverse-proxy or VPN instead.
    var dashboardAuth = builder.Environment.IsDevelopment()
        ? Array.Empty<IDashboardAuthorizationFilter>()
        : new IDashboardAuthorizationFilter[] { new LocalRequestsOnlyAuthorizationFilter() };

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Inventory Management Jobs",
        AppPath = null,
        IgnoreAntiforgeryToken = builder.Environment.IsDevelopment(),
        Authorization = dashboardAuth
    });

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
