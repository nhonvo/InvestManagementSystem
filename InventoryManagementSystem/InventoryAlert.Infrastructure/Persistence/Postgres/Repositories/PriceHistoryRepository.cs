using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class PriceHistoryRepository(AppDbContext context)
    : GenericRepository<PriceHistory>(context), IPriceHistoryRepository
{
    public async Task<IEnumerable<PriceHistory>> GetBySymbolAsync(string symbol, int limit, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.TickerSymbol == symbol)
            .OrderByDescending(x => x.RecordedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct)
    {
        // PostgreSQL optimized batched delete using subquery with LIMIT to avoid long-lived locks
        var deletedCount = 0;
        const int batchSize = 10000;

        while (!ct.IsCancellationRequested)
        {
            var batchIds = await _dbSet
                .Where(x => x.RecordedAt < cutoff)
                .Select(x => x.Id)
                .Take(batchSize)
                .ToListAsync(ct);

            if (batchIds.Count == 0) break;

            await _dbSet.Where(x => batchIds.Contains(x.Id)).ExecuteDeleteAsync(ct);
            deletedCount += batchIds.Count;
        }
    }
}
