using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Handles MarketPriceAlert trigger: fetches fresh quote from Finnhub,
/// persists a PriceHistory audit record to DynamoDB, and logs the alert.
/// </summary>
public class PriceAlertHandler(
    IFinnhubClient finnhub,
    IPriceHistoryDynamoRepository priceHistory,
    ILogger<PriceAlertHandler> logger)
    : IPriceAlertHandler
{
    private readonly IFinnhubClient _finnhub = finnhub;
    private readonly IPriceHistoryDynamoRepository _priceHistory = priceHistory;
    private readonly ILogger<PriceAlertHandler> _logger = logger;

    public async Task HandleAsync(MarketPriceAlertPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation("[PriceAlertHandler] Triggered for {Symbol}. Fetching fresh quote...", payload.Symbol);

        var quote = await _finnhub.FetchQuoteAsync(payload.Symbol, ct);

        if (quote == null || quote.CurrentPrice == 0)
        {
            _logger.LogWarning("[PriceAlertHandler] Could not fetch valid quote for {Symbol}. Skipping.", payload.Symbol);
            return;
        }

        _logger.LogWarning("🚨 PRICE ALERT | Symbol: {Symbol} | Current: ${Price:F2} | Change: {Change:F2}%",
            payload.Symbol, quote.CurrentPrice, quote.PercentChange);

        // Persist audit record so price alerts can be replayed or analysed.
        try
        {
            var entry = new PriceHistoryEntry
            {
                TickerSymbol = payload.Symbol,
                Timestamp = DateTimeOffset.UtcNow.ToString("O"),
                Price = quote.CurrentPrice ?? 0,
                ChangePercent = quote.PercentChange ?? 0,
                Ttl = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds()
            };
            await _priceHistory.SaveAsync(entry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PriceAlertHandler] Failed to persist PriceHistory for {Symbol}. Alert was still logged.", payload.Symbol);
        }
    }
}
