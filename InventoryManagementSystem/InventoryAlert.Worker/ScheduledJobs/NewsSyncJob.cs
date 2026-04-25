using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Worker.Models;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Worker.ScheduledJobs;

/// <summary>
/// Consolidated job for fetching both Global Market News and Symbol-Specific Company News.
/// </summary>
public class NewsSyncJob(
    IUnitOfWork unitOfWork,
    IFinnhubClient finnhub,
    ICompanyNewsDynamoRepository companyNewsRepo,
    IMarketNewsDynamoRepository marketNewsRepo,
    ILogger<NewsSyncJob> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly ICompanyNewsDynamoRepository _companyNewsRepo = companyNewsRepo;
    private readonly IMarketNewsDynamoRepository _marketNewsRepo = marketNewsRepo;
    private readonly ILogger<NewsSyncJob> _logger = logger;

    public async Task<JobResult> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[NewsSync] Starting consolidated news synchronization...");

            // 1. Sync Market News (Global Categories)
            int marketArticlesSaved = await SyncMarketNewsInternalAsync(ct);

            // 2. Sync Company News (Symbol-Specific)
            int companyArticlesSaved = await SyncCompanyNewsInternalAsync(ct);

            return new JobResult(JobStatus.Success, 
                $"Sync complete. Saved {marketArticlesSaved} market articles and {companyArticlesSaved} company articles.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NewsSync] Critical failure during consolidated news sync.");
            return new JobResult(JobStatus.Failed, "Consolidated news sync failed.", Error: ex);
        }
    }

    private async Task<int> SyncMarketNewsInternalAsync(CancellationToken ct)
    {
        string[] categories = { "general", "forex", "crypto", "merger" };
        int totalSaved = 0;

        foreach (var category in categories)
        {
            var articles = await _finnhub.GetMarketNewsAsync(category, ct);
            if (articles == null || articles.Count == 0) continue;

            var entries = articles
                .DistinctBy(a => a.Id)
                .Select(a => new MarketNewsDynamoEntry
                {
                    PK = $"CATEGORY#{category.ToUpperInvariant()}",
                    SK = $"TS#{a.Datetime}#ID#{a.Id}",
                    Category = category,
                    PublishedAt = DateTimeOffset.FromUnixTimeSeconds(a.Datetime).ToString("O"),
                    Headline = a.Headline ?? "No Headline",
                    Summary = a.Summary ?? string.Empty,
                    Source = a.Source ?? "Unknown",
                    Url = a.Url ?? string.Empty,
                    ImageUrl = a.Image ?? string.Empty,
                    NewsId = a.Id,
                    SyncedAt = DateTime.UtcNow.ToString("O")
                }).ToList();

            await _marketNewsRepo.BatchSaveAsync(entries, ct);
            totalSaved += entries.Count;
        }

        return totalSaved;
    }

    private async Task<int> SyncCompanyNewsInternalAsync(CancellationToken ct)
    {
        var listings = await _unitOfWork.StockListings.GetAllAsync(ct);
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var from = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        int totalSaved = 0;

        // Using Parallel fetching for company news to speed up execution for many symbols
        var options = new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct };
        
        await Parallel.ForEachAsync(listings, options, async (listing, token) =>
        {
            try
            {
                var articles = await _finnhub.GetCompanyNewsAsync(listing.TickerSymbol, from, to, token);
                if (articles.Count == 0) return;

                var entries = articles
                    .DistinctBy(a => a.Id)
                    .Select(a => new CompanyNewsDynamoEntry
                    {
                        PK = $"SYMBOL#{listing.TickerSymbol.ToUpperInvariant()}",
                        SK = $"TS#{a.Datetime}#ID#{a.Id}",
                        Symbol = listing.TickerSymbol,
                        Timestamp = a.Datetime,
                        Headline = a.Headline ?? "No Headline",
                        Summary = a.Summary ?? string.Empty,
                        Source = a.Source ?? "Unknown",
                        Url = a.Url ?? string.Empty,
                        ImageUrl = a.Image ?? string.Empty,
                        NewsId = a.Id,
                        SyncedAt = DateTime.UtcNow.ToString("O")
                    }).ToList();

                await _companyNewsRepo.BatchSaveAsync(entries, token);
                Interlocked.Add(ref totalSaved, entries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[NewsSync] Failed to fetch company news for {Symbol}: {Msg}", listing.TickerSymbol, ex.Message);
            }
        });

        return totalSaved;
    }
}
