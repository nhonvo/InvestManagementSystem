using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IWatchlistItemRepository : IGenericRepository<WatchlistItem>
{
    Task<WatchlistItem?> GetByUserAndSymbolAsync(string userId, string symbol, CancellationToken ct);
    Task<IEnumerable<WatchlistItem>> GetByUserIdAsync(string userId, CancellationToken ct);
}
