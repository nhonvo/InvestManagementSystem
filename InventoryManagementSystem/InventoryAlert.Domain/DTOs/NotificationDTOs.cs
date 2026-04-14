namespace InventoryAlert.Domain.DTOs;

public record NotificationResponse(
    Guid Id,
    string Message,
    string? TickerSymbol,
    bool IsRead,
    DateTime CreatedAt);
