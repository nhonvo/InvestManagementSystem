using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.Extensions.Caching.Distributed;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: syncs CurrentPrice for all products from Finnhub every 10 minutes.
/// Replaces the old FinnhubPriceSyncWorker from the API project.
/// Results are cached in Redis with a 60-second TTL.
/// </summary>
public class SyncPricesJob(
    InventoryDbContext db,
    IDistributedCache cache,
    IFinnhubClient finnhubClient,
    ILogger<SyncPricesJob> logger)
{
    private readonly InventoryDbContext _db = db;
    private readonly IDistributedCache _cache = cache;
    private readonly IFinnhubClient _finnhubClient = finnhubClient;
    private readonly ILogger<SyncPricesJob> _logger = logger;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        int productCount = 0;
        await foreach (var product in _db.Products.AsAsyncEnumerable().WithCancellation(ct))
        {
            productCount++;
            // Check cache first
            var cacheKey = $"product:quote:{product.TickerSymbol}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);

            decimal price;
            if (cached is not null)
            {
                price = decimal.Parse(cached);
            }
            else
            {
                var quote = await _finnhubClient.FetchQuoteAsync(product.TickerSymbol, ct);
                if (quote?.CurrentPrice is null or 0)
                {
                    _logger.LogWarning("[SyncPricesJob] No price for {Symbol}. Skipping.", product.TickerSymbol);
                    continue;
                }

                price = quote.CurrentPrice.Value;

                await _cache.SetStringAsync(cacheKey, price.ToString(),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) }, ct);
            }

            product.CurrentPrice = price;
        }

        await _db.SaveChangesAsync(ct);

        await _cache.SetStringAsync(
            "job:last-run:SyncPricesJob",
            DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct);

        _logger.LogInformation("[SyncPricesJob] Synced prices for {Count} products.", productCount);
    }
}
