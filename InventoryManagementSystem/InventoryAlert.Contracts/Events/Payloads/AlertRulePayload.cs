namespace InventoryAlert.Contracts.Events.Payloads;

public record AlertRulePayload(
    Guid RuleId,
    string Symbol,
    decimal Threshold,
    string Condition);
