namespace InventoryAlert.Contracts.Events.Payloads;

/// <summary>
/// Trigger payload: indicates a product needs a stock level check.
/// Threshold carries the product-specific minimum stock level (0 = use handler default).
/// </summary>
public record StockLowAlertPayload
{
    public int ProductId { get; init; }
    public string Symbol { get; init; } = string.Empty;
    /// <summary>Product-specific low-stock threshold. 0 means use the handler's fallback (10).</summary>
    public int Threshold { get; init; }
}
