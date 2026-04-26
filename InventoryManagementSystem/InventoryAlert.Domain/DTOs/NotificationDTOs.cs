using InventoryAlert.Domain.Common.Constants;

namespace InventoryAlert.Domain.DTOs;

public record NotificationResponse(
    Guid Id,
    string Message,
    string? TickerSymbol,
    NotificationType Type,
    NotificationSeverity Severity,
    bool IsRead,
    DateTime CreatedAt);
