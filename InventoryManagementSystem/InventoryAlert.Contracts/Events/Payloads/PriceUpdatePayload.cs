namespace InventoryAlert.Contracts.Events.Payloads;

public record PriceUpdatePayload(
    string TickerSymbol,
    decimal Price,
    decimal Change,
    decimal ChangePercent,
    string Timestamp);
