namespace InventoryAlert.Contracts.Events.Payloads;

/// <summary>
/// Trigger payload: indicates a symbol needs a price check.
/// No longer carries the price data itself.
/// </summary>
public record MarketPriceAlertPayload
{
    public int ProductId { get; init; }
    public string Symbol { get; init; } = string.Empty;
}
