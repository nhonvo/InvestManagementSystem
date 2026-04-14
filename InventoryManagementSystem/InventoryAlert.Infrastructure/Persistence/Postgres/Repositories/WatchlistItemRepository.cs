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
}
