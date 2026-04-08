using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Api.Domain.Interfaces;

public interface IWatchlistRepository : IGenericRepository<Watchlist>
{
    Task<bool> ExistsAsync(string userId, string symbol, CancellationToken ct = default);
    Task<IEnumerable<Watchlist>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Watchlist?> GetAsync(string userId, string symbol, CancellationToken ct = default);
}
