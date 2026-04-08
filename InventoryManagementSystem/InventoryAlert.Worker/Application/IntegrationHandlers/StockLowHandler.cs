using InventoryAlert.Contracts.Events.Payloads;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Worker.Application.Interfaces.Handlers;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Trigger handler: verifies the current stock level in the DB 
/// and logs a warning if it's still below threshold.
/// </summary>
public class StockLowHandler(InventoryDbContext db, ILogger<StockLowHandler> logger) : IStockLowHandler
{
    private readonly InventoryDbContext _db = db;
    private readonly ILogger<StockLowHandler> _logger = logger;

    public async Task HandleAsync(StockLowAlertPayload payload, CancellationToken ct = default)
    {
        // Structured Logging: pass IDs as parameters, not just in string
        _logger.LogInformation("[StockLowHandler] Triggered for ProductId {ProductId}. Verifying current stock level...", payload.ProductId);

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == payload.ProductId, ct);

        if (product == null)
        {
            _logger.LogWarning("[StockLowHandler] Product {ProductId} not found in database.", payload.ProductId);
            return;
        }

        if (product.StockCount <= 10) // Fallback fixed threshold for simulation
        {
            _logger.LogWarning("[StockLowHandler] VERIFIED LOW STOCK for {Symbol} (ID: {ProductId}). Current: {Stock}", 
                product.TickerSymbol, product.Id, product.StockCount);
        }
        else
        {
            _logger.LogInformation("[StockLowHandler] Stock level for {Symbol} has recovered to {Stock}. No alert needed.", 
                product.TickerSymbol, product.StockCount);
        }
    }
}
