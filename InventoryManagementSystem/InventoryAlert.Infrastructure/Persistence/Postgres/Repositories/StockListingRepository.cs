using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class StockListingRepository(AppDbContext context)
    : GenericRepository<StockListing>(context), IStockListingRepository
{
    public async Task<StockListing?> FindBySymbolAsync(string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TickerSymbol == symbol, ct);
    }

    public async Task<IEnumerable<StockListing>> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync(ct);

        return await _dbSet.AsNoTracking()
            .Where(x => EF.Functions.ILike(x.Name, $"%{query}%") ||
                        EF.Functions.ILike(x.TickerSymbol, $"%{query}%"))
            .ToListAsync(ct);
    }
}
