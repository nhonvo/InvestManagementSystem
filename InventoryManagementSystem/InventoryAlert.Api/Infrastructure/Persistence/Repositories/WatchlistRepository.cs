using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories;

public class WatchlistRepository(InventoryDbContext context)
    : GenericRepository<Watchlist>(context), IWatchlistRepository
{
    private readonly InventoryDbContext _context = context;

    public async Task<bool> ExistsAsync(string userId, string symbol, CancellationToken ct = default)
    {
        return await _context.Watchlists
            .AnyAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
    }

    public async Task<IEnumerable<Watchlist>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Watchlists
            .AsNoTracking()
            .Include(w => w.Product)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(ct);
    }

    public async Task<Watchlist?> GetAsync(string userId, string symbol, CancellationToken ct = default)
    {
        return await _context.Watchlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Symbol == symbol, ct);
    }
}
