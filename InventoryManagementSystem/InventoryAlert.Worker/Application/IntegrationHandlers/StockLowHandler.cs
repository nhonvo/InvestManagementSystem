using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Worker.Application.Interfaces.Handlers;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Trigger handler: verifies the current stock level in the DB
/// and logs a warning if it's still below the product's configured alert threshold.
/// </summary>
public class StockLowHandler(IProductRepository products, ILogger<StockLowHandler> logger) : IStockLowHandler
{
    private readonly IProductRepository _products = products;
    private readonly ILogger<StockLowHandler> _logger = logger;

    public async Task HandleAsync(StockLowAlertPayload payload, CancellationToken ct = default)
    {
        _logger.LogInformation("[StockLowHandler] Triggered for ProductId {ProductId}. Verifying current stock level...", payload.ProductId);

        var product = await _products.GetByIdAsync(payload.ProductId, ct);

        if (product == null)
        {
            _logger.LogWarning("[StockLowHandler] Product {ProductId} not found in database.", payload.ProductId);
            return;
        }

        // Use the product's own configured low-stock threshold (default 0 means use payload threshold).
        // PriceAlertThreshold is a price field; stock threshold lives on payload or a dedicated field.
        // If the entity gains a StockAlertThreshold property later, switch to product.StockAlertThreshold.
        var threshold = payload.Threshold > 0 ? payload.Threshold : 10;

        if (product.StockCount <= threshold)
        {
            _logger.LogWarning("[StockLowHandler] VERIFIED LOW STOCK for {Symbol} (ID: {ProductId}). Current: {Stock}. Threshold: {Threshold}",
                product.TickerSymbol, product.Id, product.StockCount, threshold);

            // Notification dispatch: log the alert here. A dedicated INotificationService
            // (e.g. TelegramBotService) should subscribe to low-stock events in the pipeline
            // rather than being tightly coupled into this handler.
        }
        else
        {
            _logger.LogInformation("[StockLowHandler] Stock level for {Symbol} has recovered to {Stock}. No alert needed.",
                product.TickerSymbol, product.StockCount);
        }
    }
}
