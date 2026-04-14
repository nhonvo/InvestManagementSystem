using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class NotificationRepository(AppDbContext context)
    : GenericRepository<Notification>(context), INotificationRepository
{
    public async Task<PagedResult<Notification>> GetByUserPagedAsync(string userId, bool onlyUnread, int page, int pageSize, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        var query = _dbSet.AsNoTracking().Where(x => x.UserId == userGuid);

        if (onlyUnread)
        {
            query = query.Where(x => !x.IsRead);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Notification>
        {
            Items = items,
            TotalItems = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        return await _dbSet.CountAsync(x => x.UserId == userGuid && !x.IsRead, ct);
    }

    public async Task<int> MarkAllReadAsync(string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        return await _dbSet
            .Where(x => x.UserId == userGuid && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
