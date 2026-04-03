using InventoryAlert.Api.Application.Interfaces;

namespace InventoryAlert.Api.Infrastructure.Workers
{
    public class FinnhubSyncWorker(AppSettings appSettings, IServiceScopeFactory scopeFactory) : BackgroundService
    {
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(appSettings.MinuteSyncCurrentPrice);
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            using PeriodicTimer timer = new(_interval);
            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                using var scope = scopeFactory.CreateScope();
                var productService = scope.ServiceProvider.GetRequiredService<IProductService>();
                await productService.SyncCurrentPricesAsync(cancellationToken);
            }
        }
    }
}
