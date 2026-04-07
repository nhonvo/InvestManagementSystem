using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Persistence.Repositories;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Trigger handler: fetches the last 24h of headlines for a symbol 
/// and persists them to DynamoDB.
/// </summary>
public class NewsHandler(
    InventoryDbContext db, 
    INewsDynamoRepository newsRepo, 
    IFinnhubClient finnhub,
    ILogger<NewsHandler> logger)
    : INewsHandler
{
    private readonly InventoryDbContext _db = db;
    private readonly INewsDynamoRepository _newsRepo = newsRepo;
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly ILogger<NewsHandler> _logger = logger;

    public async Task HandleAsync(CompanyNewsAlertPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation("[NewsHandler] Triggered for {Symbol}. Fetching latest news from Finnhub...", payload.Symbol);

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TickerSymbol == payload.Symbol, ct);

        if (product is null)
        {
            _logger.LogWarning("[NewsHandler] No product found for symbol {Symbol}. Skipping.", payload.Symbol);
            return;
        }

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        var articles = await _finnhub.FetchNewsAsync(payload.Symbol, yesterday, today, ct);

        if (articles == null || articles.Count == 0)
        {
            _logger.LogInformation("[NewsHandler] No new articles found for {Symbol}.", payload.Symbol);
            return;
        }

        foreach (var article in articles)
        {
            var entry = MapToDynamoEntry(payload.Symbol, article);
            await _newsRepo.SaveAsync(entry, ct);
        }

        _logger.LogInformation("[NewsHandler] Successfully processed {Count} articles for {Symbol}.", articles.Count, payload.Symbol);
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
