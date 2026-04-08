namespace InventoryAlert.Contracts.Events.Payloads;

public record RecommendationUpdatedPayload(
    string Symbol,
    string Period,
    int StrongBuy,
    int Buy,
    int Hold,
    int Sell,
    int StrongSell);
