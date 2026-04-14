using InventoryAlert.Domain.Entities.Dynamodb;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Worker.IntegrationEvents.Handlers;

public class CompanyNewsAlertHandler(
    IFinnhubClient finnhubClient,
    ICompanyNewsDynamoRepository newsRepo,
    ILogger<CompanyNewsAlertHandler> logger)
{
    private readonly IFinnhubClient _finnhubClient = finnhubClient;
    private readonly ICompanyNewsDynamoRepository _newsRepo = newsRepo;
    private readonly ILogger<CompanyNewsAlertHandler> _logger = logger;

    public async Task HandleAsync(CompanyNewsAlertPayload payload, CancellationToken ct)
    {
        _logger.LogInformation("[CompanyNewsAlertHandler] Pulling latest company news for {Symbol}", payload.Symbol);

        try
        {
            // Sync window: Last 7 days to cover trailing events
            var to = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var from = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

            var articles = await _finnhubClient.GetCompanyNewsAsync(payload.Symbol, from, to, ct);
            if (articles.Count == 0)
            {
                _logger.LogInformation("[CompanyNewsAlertHandler] No news found for {Symbol} in the given range.", payload.Symbol);
                return;
            }

            var entries = articles
                .DistinctBy(a => a.Id)
                .Select(a => new CompanyNewsDynamoEntry
            {
                PK = $"SYMBOL#{payload.Symbol.ToUpperInvariant()}",
                SK = $"TS#{a.Datetime}#ID#{a.Id}",
                Symbol = payload.Symbol,
                Timestamp = a.Datetime,
                Headline = a.Headline ?? "No Headline",
                Summary = a.Summary ?? string.Empty,
                Source = a.Source ?? "Unknown",
                Url = a.Url ?? string.Empty,
                ImageUrl = a.Image ?? string.Empty,
                NewsId = a.Id,
                SyncedAt = DateTime.UtcNow.ToString("O")
            }).ToList();

            // DynamoDB batch write (limit 25 per call handled by internal repo implementation)
            await _newsRepo.BatchSaveAsync(entries, ct);
            
            _logger.LogInformation("[CompanyNewsAlertHandler] Successfully persisted {Count} articles for {Symbol}", entries.Count, payload.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CompanyNewsAlertHandler] Pipeline failure for {Symbol}", payload.Symbol);
            throw; // Rethrow to trigger retry logic in the message processor if applicable
        }
    }
}
