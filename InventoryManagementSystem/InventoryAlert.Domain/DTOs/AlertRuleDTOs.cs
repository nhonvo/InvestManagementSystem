using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.DTOs;

public record AlertRuleRequest(
    string TickerSymbol,
    AlertCondition Condition,
    decimal TargetValue,
    bool TriggerOnce);

public record AlertRuleResponse(
    Guid Id,
    string TickerSymbol,
    AlertCondition Condition,
    decimal TargetValue,
    bool IsActive,
    bool TriggerOnce,
    DateTime? LastTriggeredAt);

public record ToggleAlertRequest(bool IsActive);
