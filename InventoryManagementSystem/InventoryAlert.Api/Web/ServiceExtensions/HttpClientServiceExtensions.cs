using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Web.Configuration;

namespace InventoryAlert.Api.Web.ServiceExtensions;

public static class HttpClientServiceExtensions
{
    /// <summary>
    /// Centralizes all HTTP Client factory configurations
    /// such as resilience parameters, connection pooling, and base addresses.
    /// </summary>
    public static IServiceCollection AddConfiguredHttpClients(this IServiceCollection services, AppSettings settings)
    {
        // ── Finnhub Configuration ────────────────────────────────────────────
        services.AddHttpClient(ApplicationConstants.HttpClientNames.Finnhub, client =>
        {
            client.BaseAddress = new Uri(settings.Finnhub.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2) // Prevent stale DNS connections
        });

        // ── Telegram Configuration ───────────────────────────────────────────
        services.AddHttpClient(ApplicationConstants.HttpClientNames.Telegram, client => 
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });

        return services;
    }
}
