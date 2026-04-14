using Amazon.DynamoDBv2;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FluentValidation;
using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Domain.Validators;
using InventoryAlert.Infrastructure.Caching;
using InventoryAlert.Infrastructure.External.Finnhub;
using InventoryAlert.Infrastructure.Messaging;
using InventoryAlert.Infrastructure.Persistence.DynamoDb.Repositories;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using InventoryAlert.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using StackExchange.Redis;

namespace InventoryAlert.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppSettings settings)
    {
        // ── Configured HttpClients ─────────────────────────────────────────
        services.AddHttpClient(ApplicationConstants.HttpClientNames.Finnhub, client =>
        {
            client.BaseAddress = new Uri(settings.Finnhub.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        services.AddHttpClient();

        // ── Persistence: Postgres ──────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                settings.Database.DefaultConnection,
                b => b.MigrationsAssembly("InventoryAlert.Infrastructure")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IStockListingRepository, StockListingRepository>();
        services.AddScoped<IWatchlistItemRepository, WatchlistItemRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IStockMetricRepository, StockMetricRepository>();
        services.AddScoped<IEarningsSurpriseRepository, EarningsSurpriseRepository>();
        services.AddScoped<IRecommendationTrendRepository, RecommendationTrendRepository>();
        services.AddScoped<IInsiderTransactionRepository, InsiderTransactionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // ── Persistence: DynamoDb ──────────────────────────────────────────
        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            var config = new AmazonDynamoDBConfig { ServiceURL = settings.Aws.EndpointUrl };
            return new AmazonDynamoDBClient(config);
        });

        services.AddScoped<IMarketNewsDynamoRepository, MarketNewsDynamoRepository>();
        services.AddScoped<ICompanyNewsDynamoRepository, CompanyNewsDynamoRepository>();

        // ── Messaging ──────────────────────────────────────────────────────
        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var config = new AmazonSQSConfig { ServiceURL = settings.Aws.EndpointUrl };
            return new AmazonSQSClient(config);
        });

        services.AddScoped<IQueueService, SqsQueueService>();

        services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
        {
            var config = new AmazonSimpleNotificationServiceConfig { ServiceURL = settings.Aws.EndpointUrl };
            return new AmazonSimpleNotificationServiceClient(config);
        });
        services.AddScoped<IEventPublisher, SnsEventPublisher>();

        // ── External Services ──────────────────────────────────────────────
        services.AddScoped<IFinnhubClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient(ApplicationConstants.HttpClientNames.Finnhub);
            return new FinnhubClient(new RestClient(httpClient), settings, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FinnhubClient>>());
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(settings.Redis.ConnectionString));
        services.AddScoped<IRedisHelper, RedisHelper>();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = settings.Redis.ConnectionString;
            options.InstanceName = "InventoryAlert_";
        });

        // ── Application Services ───────────────────────────────────────────
        services.AddValidatorsFromAssemblyContaining<CreatePositionRequestValidator>();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationProvider, CorrelationProvider>();

        return services;
    }
}
