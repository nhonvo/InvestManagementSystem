namespace InventoryAlert.Domain.Events.Payloads;

/// <summary>
/// Trigger payload: indicates a symbol needs a price check or has changed.
/// No longer carries user-specific IDs as it triggers multi-user evaluation.
/// </summary>
public record MarketPriceAlertPayload
{
    public string Symbol { get; init; } = string.Empty;
    public decimal NewPrice { get; init; }
}
