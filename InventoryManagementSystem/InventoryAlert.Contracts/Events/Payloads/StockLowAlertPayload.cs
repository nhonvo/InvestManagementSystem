namespace InventoryAlert.Contracts.Events.Payloads;

/// <summary>
/// Trigger payload: indicates a product needs a stock level check.
/// </summary>
public record StockLowAlertPayload
{
    public int ProductId { get; init; }
    public string Symbol { get; init; } = string.Empty;
}
