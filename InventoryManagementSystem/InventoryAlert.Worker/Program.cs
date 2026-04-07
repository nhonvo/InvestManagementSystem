using Amazon.DynamoDBv2;
using Amazon.SQS;
using Hangfire;
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

// ─── Serilog bootstrap (identical pattern to the Api) ─────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
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
    var settings = builder.Configuration.Get<WorkerSettings>()
        ?? throw new InvalidOperationException("WorkerSettings configuration is missing.");

    builder.Services.AddSingleton(settings);

    // ── EF Core (shared PostgreSQL schema with API) ───────────────────────────
    builder.Services.AddDbContext<InventoryDbContext>(opts =>
        opts.UseNpgsql(settings.Database.DefaultConnection));

    // ── Redis Distributed Cache ───────────────────────────────────────────────
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(settings.Redis.Connection));

    builder.Services.AddStackExchangeRedisCache(opts =>
    {
        opts.Configuration = settings.Redis.Connection;
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

    // ── Repositories (Worker Specific) ────────────────────────────────────────
    builder.Services.AddScoped<IEventLogDynamoRepository, EventLogDynamoRepository>();
    builder.Services.AddScoped<INewsDynamoRepository, NewsDynamoRepository>();

    // ── Hangfire Jobs (Scoped) ────────────────────────────────────────────────
    builder.Services.AddScoped<PollSqsJob>();
    builder.Services.AddScoped<SyncPricesJob>();
    builder.Services.AddScoped<NewsCheckJob>();

    // ── Background Task Queue (In-Memory) ─────────────────────────────────────
    builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx =>
    {
        var capacity = ctx.GetRequiredService<WorkerSettings>().SqsPolling.QueueCapacity;
        return new BackgroundTaskQueue(capacity);
    });

    // ── Scheduled & Background Services ───────────────────────────────────────
    builder.Services.AddHostedService<JobSchedulerService>();
    builder.Services.AddHostedService<QueuedHostedService>();

    // One-time registration for the native polling logic
    builder.Services.AddScoped<IProcessQueueJob, ProcessQueueJob>();

    if (settings.SqsPolling.UseNativeWorker)
    {
        builder.Services.AddHostedService<NativeSqsWorker>();
    }

    var app = builder.Build();

    // Hangfire UI
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        DashboardTitle = "Inventory Management Jobs",
        AppPath = null,
        IgnoreAntiforgeryToken = true,
        Authorization = []
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
