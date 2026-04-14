namespace InventoryAlert.Domain.Events.Payloads;

public record LowHoldingsAlertPayload(
    Guid UserId,
    string TickerSymbol,
    decimal Threshold,
    decimal CurrentQuantity);
