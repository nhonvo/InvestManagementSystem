namespace InventoryAlert.Contracts.Events.Payloads;

public record EarningsPayload(
    string Symbol,
    string Period,
    decimal Actual,
    decimal Estimate,
    decimal Surprise,
    decimal SurprisePercent);
