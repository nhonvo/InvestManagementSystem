using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using InventoryAlert.Worker.Infrastructure.External.Finnhub;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Handles MarketPriceAlert trigger: fetches fresh quote from Finnhub 
/// and logs/dispatches notification.
/// </summary>
public class PriceAlertHandler(IFinnhubClient finnhub, ILogger<PriceAlertHandler> logger)
    : IPriceAlertHandler
{
    private readonly IFinnhubClient _finnhub = finnhub;
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

        // Using Structured Logging instead of string interpolation in templates
        _logger.LogWarning("🚨 PRICE ALERT | Symbol: {Symbol} | Current: ${Price:F2} | Change: {Change:F2}%", 
            payload.Symbol, quote.CurrentPrice, quote.PercentChange);
    }
}
