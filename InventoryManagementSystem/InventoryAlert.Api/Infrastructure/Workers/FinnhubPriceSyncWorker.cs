using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Configuration;

namespace InventoryAlert.Api.Infrastructure.Workers
{
    /// <summary>
    /// Background worker that periodically syncs the CurrentPrice of all products
    /// from Finnhub API. Interval is configured via MinuteSyncCurrentPrice in appsettings.
    /// </summary>
    public class FinnhubPriceSyncWorker(AppSettings appSettings, IServiceScopeFactory scopeFactory) : BackgroundService
    {
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(appSettings.MinuteSyncCurrentPrice);

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new(_interval);
            while (!cancellationToken.IsCancellationRequested
                   && await timer.WaitForNextTickAsync(cancellationToken))
            {
                using var scope = scopeFactory.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                await productService.SyncCurrentPricesAsync(cancellationToken);
            }
        }
    }
}
