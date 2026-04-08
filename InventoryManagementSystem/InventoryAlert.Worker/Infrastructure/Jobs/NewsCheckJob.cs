using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: fetches /company-news from Finnhub every hour.
/// Caches latest headline in Redis to avoid duplicate processing.
/// Triggers NewsHandler with a thin payload (Symbol only).
/// </summary>
public class NewsCheckJob(
    InventoryDbContext db,
    IDistributedCache cache,
    IFinnhubClient finnhubClient,
    INewsHandler newsHandler,
    ILogger<NewsCheckJob> logger)
{
    private readonly InventoryDbContext _db = db;
    private readonly IDistributedCache _cache = cache;
    private readonly IFinnhubClient _finnhubClient = finnhubClient;
    private readonly INewsHandler _newsHandler = newsHandler;
    private readonly ILogger<NewsCheckJob> _logger = logger;

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var products = await _db.Products.AsNoTracking().ToListAsync(ct);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var weekAgo = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = ct };

        await Parallel.ForEachAsync(products, parallelOptions, async (product, token) =>
        {
            // Basic throttle to avoid 60/min API limit
            await Task.Delay(1000, token);

            try
            {
                var articles = await _finnhubClient.FetchNewsAsync(product.TickerSymbol, weekAgo, today, token);
                if (articles is null || articles.Count == 0) return;

                var latest = articles[0];

                var cacheKey = $"news:{product.TickerSymbol}:latest";
                var cachedHeadline = await _cache.GetStringAsync(cacheKey, token);

                if (cachedHeadline == latest.Headline) return;  // already processed

                // THIN PAYLOAD: Only send Symbol trigger
                var payload = new CompanyNewsAlertPayload
                {
                    Symbol = product.TickerSymbol
                };

                // Trigger the handler (which will re-fetch and filter in a transactionally safe way)
                await _newsHandler.HandleAsync(payload, token);

                // Update cache to avoid redundant triggers
                await _cache.SetStringAsync(cacheKey, latest.Headline ?? string.Empty,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) }, token);

                _logger.LogInformation("[NewsCheckJob] Triggered NewsHandler for {Symbol} due to new headline: {Headline}",
                    product.TickerSymbol, latest.Headline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NewsCheckJob] Error checking news for {Symbol}.", product.TickerSymbol);
            }
        });

        await _cache.SetStringAsync(
            "job:last-run:NewsCheckJob",
            DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct);
    }
}
