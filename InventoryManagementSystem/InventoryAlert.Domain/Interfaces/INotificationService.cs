using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(Guid userId, string message, string? symbol = null, Guid? alertRuleId = null, CancellationToken ct = default);
    Task<PagedResult<NotificationResponse>> GetPagedAsync(string userId, bool onlyUnread, int page, int pageSize, CancellationToken ct);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct);
    Task MarkReadAsync(Guid id, string userId, CancellationToken ct);
    Task<int> MarkAllReadAsync(string userId, CancellationToken ct);
    Task DismissAsync(Guid id, string userId, CancellationToken ct);
}
