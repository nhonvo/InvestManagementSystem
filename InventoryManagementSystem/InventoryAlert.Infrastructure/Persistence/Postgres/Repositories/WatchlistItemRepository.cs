using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class WatchlistItemRepository(AppDbContext context)
    : GenericRepository<WatchlistItem>(context), IWatchlistItemRepository
{
    public async Task<WatchlistItem?> GetByUserAndSymbolAsync(string userId, string symbol, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userGuid && x.TickerSymbol == symbol, ct);
    }

    public async Task<IEnumerable<WatchlistItem>> GetByUserIdAsync(string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userGuid)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<WatchlistItem> Items, int TotalCount)> GetPagedByUserIdAsync(string userId, int pageNumber, int pageSize, string? search, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);
        var query = _dbSet.AsNoTracking()
            .Where(x => x.UserId == userGuid);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => EF.Functions.ILike(x.TickerSymbol, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.TickerSymbol)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
