using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: syncs CurrentPrice for all products from Finnhub every 10 minutes.
/// Processes products in pages of 50 to avoid tracking all entities in memory simultaneously.
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

    private const int PageSize = 50;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        int totalSynced = 0;
        int page = 0;

        while (true)
        {
            // Load products in pages — prevents all EF entities being tracked simultaneously.
            var products = await _db.Products
                .OrderBy(p => p.Id)
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToListAsync(ct);

            if (products.Count == 0) break;

            foreach (var product in products)
            {
                ct.ThrowIfCancellationRequested();

                var cacheKey = $"product:quote:{product.TickerSymbol}";
                var cached = await _cache.GetStringAsync(cacheKey, ct);

                decimal price;
                if (cached is not null)
                {
                    // Use InvariantCulture to avoid locale-dependent decimal separators.
                    if (!decimal.TryParse(cached, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out price))
                    {
                        _logger.LogWarning("[SyncPricesJob] Cached price for {Symbol} was non-parseable. Skipping.", product.TickerSymbol);
                        continue;
                    }
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

                    await _cache.SetStringAsync(
                        cacheKey,
                        price.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) },
                        ct);
                }

                product.CurrentPrice = price;
                totalSynced++;
            }

            // Save each page independently — partial cancellation only loses the current page,
            // not all previously committed pages.
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear(); // detach to prevent ever-growing tracked graph

            page++;
        }

        await _cache.SetStringAsync(
            "job:last-run:SyncPricesJob",
            DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
            ct);

        _logger.LogInformation("[SyncPricesJob] Synced prices for {Count} products.", totalSynced);
    }
}
