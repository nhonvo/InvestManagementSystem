namespace InventoryAlert.Api.Application.DTOs;

public record AlertRuleRequest(
    string Symbol,
    string Field,       // price | volume | change_pct
    string Operator,    // gt | lt | gte | lte | eq
    decimal Threshold,
    string NotifyChannel = "telegram");

public record AlertRuleResponse(
    Guid Id,
    string UserId,
    string Symbol,
    string Field,
    string Operator,
    decimal Threshold,
    string NotifyChannel,
    bool IsActive,
    DateTime? LastTriggeredAt,
    DateTime CreatedAt);
