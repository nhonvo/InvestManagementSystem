using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace InventoryAlert.Worker.Infrastructure.Jobs;

/// <summary>
/// Hangfire job: fetches /company-news from Finnhub every hour.
/// Deduplicates by FinnhubId (not headline text) to handle identical-text articles.
/// Passes already-fetched articles directly to the repository to avoid a second Finnhub call.
/// Throttles using SemaphoreSlim(2) to stay within 60 req/min without sleeping all threads.
/// </summary>
public class NewsCheckJob(
    InventoryDbContext db,
    IDistributedCache cache,
    IFinnhubClient finnhubClient,
    INewsDynamoRepository newsRepo,
    ILogger<NewsCheckJob> logger)
{
    private readonly InventoryDbContext _db = db;
    private readonly IDistributedCache _cache = cache;
    private readonly IFinnhubClient _finnhubClient = finnhubClient;
    private readonly INewsDynamoRepository _newsRepo = newsRepo;
    private readonly ILogger<NewsCheckJob> _logger = logger;

    // Allow max 2 concurrent Finnhub calls without burning threads via Task.Delay.
    private static readonly SemaphoreSlim _throttle = new(2, 2);

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var products = await _db.Products.AsNoTracking().ToListAsync(ct);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var weekAgo = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

        var tasks = products.Select(product => ProcessProductAsync(product.TickerSymbol, weekAgo, today, ct));
        await Task.WhenAll(tasks);

        await _cache.SetStringAsync(
            "job:last-run:NewsCheckJob",
            DateTime.UtcNow.ToString("O"),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
            ct);
    }

    private async Task ProcessProductAsync(string symbol, string weekAgo, string today, CancellationToken ct)
    {
        await _throttle.WaitAsync(ct);
        try
        {
            var articles = await _finnhubClient.FetchNewsAsync(symbol, weekAgo, today, ct);
            if (articles is null || articles.Count == 0) return;

            // Dedup key uses FinnhubId (not headline string) to handle identical-text articles.
            var latestId = articles[0].Id.ToString();
            var cacheKey = $"news:{symbol}:latest-id";
            var cachedId = await _cache.GetStringAsync(cacheKey, ct);

            if (cachedId == latestId) return; // already processed this batch

            // Persist articles directly — no second Finnhub round-trip.
            var entries = articles.Select(a => MapToDynamoEntry(symbol, a)).ToList();
            await _newsRepo.BatchSaveAsync(entries, ct);

            await _cache.SetStringAsync(cacheKey, latestId,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
                ct);

            _logger.LogInformation("[NewsCheckJob] Persisted {Count} articles for {Symbol}.", entries.Count, symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NewsCheckJob] Error checking news for {Symbol}.", symbol);
        }
        finally
        {
            _throttle.Release();
        }
    }

    private static NewsDynamoEntry MapToDynamoEntry(string symbol, NewsArticle article) => new()
    {
        TickerSymbol = symbol,
        PublishedAt = DateTimeOffset.FromUnixTimeSeconds(article.Datetime).ToString("O"),
        Headline = article.Headline ?? "No Headline",
        Summary = article.Summary ?? string.Empty,
        Source = article.Source ?? "Unknown",
        Url = article.Url ?? string.Empty,
        ImageUrl = article.Image ?? string.Empty,
        FinnhubId = article.Id,
        Ttl = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
    };
}
