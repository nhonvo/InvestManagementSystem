using Amazon.SimpleNotificationService;
using InventoryAlert.Api.Domain.Constants;
using Amazon.SQS;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Api.Infrastructure.External;
using InventoryAlert.Api.Infrastructure.Messaging;
using InventoryAlert.Api.Infrastructure.Notifications;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Persistence.Repositories;
using InventoryAlert.Api.Infrastructure.Persistence.Repositories;
using InventoryAlert.Api.Web.Configuration;
using Microsoft.EntityFrameworkCore;
using RestSharp;

namespace InventoryAlert.Api.Web.ServiceExtensions;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure concerns: DB, repositories, external clients,
    /// messaging (SNS), and background workers.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        AppSettings settings)
    {
        // ── Persistence ─────────────────────────────────────────────────────
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(settings.Database.DefaultConnection, b => b.MigrationsAssembly("InventoryAlert.Api")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IEventLogRepository, EventLogRepository>();

        // ── External HTTP clients ────────────────────────────────────────────
        services.AddConfiguredHttpClients(settings);

        services.AddScoped<IFinnhubClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient(ApplicationConstants.HttpClientNames.Finnhub);
            var logger = sp.GetRequiredService<ILogger<FinnhubClient>>();
            return new FinnhubClient(new RestClient(httpClient), settings, logger);
        });

        // ── AWS / SNS ────────────────────────────────────────────────────────
        services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
        {
            var config = new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = settings.Aws.EndpointUrl
            };
            return new AmazonSimpleNotificationServiceClient(config);
        });

        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var config = new AmazonSQSConfig { ServiceURL = settings.Aws.EndpointUrl };
            return new AmazonSQSClient(config);
        });

        services.AddSingleton<Amazon.DynamoDBv2.IAmazonDynamoDB>(_ =>
        {
            var config = new Amazon.DynamoDBv2.AmazonDynamoDBConfig { ServiceURL = settings.Aws.EndpointUrl };
            return new Amazon.DynamoDBv2.AmazonDynamoDBClient(config);
        });

        services.AddScoped<EventLogDynamoRepository>();
        services.AddScoped<NewsDynamoRepository>();
        services.AddScoped<IEventLogQuery, DynamoDbEventLogQuery>();

        // ── Messaging / Notifications ────────────────────────────────────────
        services.AddScoped<IEventPublisher, SnsEventPublisher>();

        // Telegram HttpClient (used by TelegramAlertNotifier; no base address needed — URL built per call)
        services.AddScoped<IAlertNotifier, TelegramAlertNotifier>();

        // NOTE: FinnhubPriceSyncWorker has been retired.
        // Price syncing is now owned by SyncPricesJob in InventoryAlert.Worker.
        // See EVENT_DRIVEN_PLAN.md Phase C.

        return services;
    }
}
