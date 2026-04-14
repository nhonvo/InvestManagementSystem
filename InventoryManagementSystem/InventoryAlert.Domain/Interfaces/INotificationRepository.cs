using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<PagedResult<Notification>> GetByUserPagedAsync(string userId, bool onlyUnread, int page, int pageSize, CancellationToken ct);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct);
    Task<int> MarkAllReadAsync(string userId, CancellationToken ct);
}
