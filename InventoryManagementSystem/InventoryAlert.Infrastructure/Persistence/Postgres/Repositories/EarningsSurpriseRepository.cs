using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class EarningsSurpriseRepository(AppDbContext context) : IEarningsSurpriseRepository
{
    private readonly DbSet<EarningsSurprise> _dbSet = context.EarningsSurprises;

    public async Task<IEnumerable<EarningsSurprise>> GetBySymbolAsync(string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.TickerSymbol == symbol)
            .OrderByDescending(x => x.Period)
            .ToListAsync(ct);
    }

    public async Task UpsertRangeAsync(IEnumerable<EarningsSurprise> earnings, CancellationToken ct)
    {
        foreach (var item in earnings)
        {
            var existing = await _dbSet
                .FirstOrDefaultAsync(x => x.TickerSymbol == item.TickerSymbol && x.Period == item.Period, ct);

            if (existing == null)
            {
                await _dbSet.AddAsync(item, ct);
            }
            else
            {
                item.Id = existing.Id;
                context.Entry(existing).CurrentValues.SetValues(item);
            }
        }
    }
}
