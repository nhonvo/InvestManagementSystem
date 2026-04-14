using InventoryAlert.Api.Services;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure;

namespace InventoryAlert.Api.ServiceExtensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddWebApiInfrastructure(
        this IServiceCollection services,
        AppSettings settings)
    {
        // ── Shared Infrastructure (Context, Repos, AWS, Redis, Messaging) ──
        services.AddInfrastructure(settings);

        // ── API-Specific Services ─────────────────────────────────────────
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAlertRuleService, AlertRuleService>();
        services.AddScoped<IStockDataService, StockDataService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IWatchlistService, WatchlistService>();

        return services;
    }
}
