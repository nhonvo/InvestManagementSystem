using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Worker.IntegrationEvents.Handlers;

public class SyncMarketNewsHandler(
    IFinnhubClient finnhub,
    IMarketNewsDynamoRepository newsRepo,
    ILogger<SyncMarketNewsHandler> logger)
{
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IMarketNewsDynamoRepository _newsRepo = newsRepo;
    private readonly ILogger<SyncMarketNewsHandler> _logger = logger;

    public async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("[SyncMarketNewsHandler] Starting market news synchronization...");

        try
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

                await _newsRepo.BatchSaveAsync(entries, ct);
                totalSaved += entries.Count;
            }

            _logger.LogInformation("[SyncMarketNewsHandler] Successfully persisted {TotalSaved} market news articles across all categories.", totalSaved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SyncMarketNewsHandler] Critical failure during news synchronization.");
            throw; // Hangfire handles retries
        }
    }
}
