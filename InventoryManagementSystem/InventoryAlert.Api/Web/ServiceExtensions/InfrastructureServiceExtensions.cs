using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Api.Infrastructure.External;
using InventoryAlert.Api.Infrastructure.External.Interfaces;
using InventoryAlert.Api.Infrastructure.Persistence;
using InventoryAlert.Api.Infrastructure.Persistence.Repositories;
using InventoryAlert.Api.Infrastructure.Workers;
using InventoryAlert.Api.Web.Configuration;
using Microsoft.EntityFrameworkCore;
using RestSharp;

namespace InventoryAlert.Api.Web.ServiceExtensions;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure concerns: DB, repositories, external clients, background workers.
    /// Accepts <see cref="AppSettings"/> so callers don't need to re-read config here.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        AppSettings settings)
    {
        // ── Persistence ────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(settings.Database.DefaultConnection));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // ── External HTTP clients ──────────────────────────────────────────────
        services.AddHttpClient("Finnhub", client =>
        {
            client.BaseAddress = new Uri(settings.Finnhub.ApiBaseUrl);
        });

        services.AddScoped<IFinnhubClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = factory.CreateClient("Finnhub");
            return new FinnhubClient(new RestClient(httpClient), settings);
        });

        // ── Background workers ─────────────────────────────────────────────────
        services.AddHostedService<FinnhubPriceSyncWorker>();

        return services;
    }
}
