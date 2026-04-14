using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class RecommendationTrendRepository(AppDbContext context) : IRecommendationTrendRepository
{
    private readonly DbSet<RecommendationTrend> _dbSet = context.RecommendationTrends;

    public async Task<IEnumerable<RecommendationTrend>> GetBySymbolAsync(string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.TickerSymbol == symbol)
            .OrderByDescending(x => x.Period)
            .ToListAsync(ct);
    }

    public async Task UpsertRangeAsync(IEnumerable<RecommendationTrend> recommendations, CancellationToken ct)
    {
        foreach (var item in recommendations)
        {
            var existing = await _dbSet
                .FirstOrDefaultAsync(x => x.TickerSymbol == item.TickerSymbol && x.Period == item.Period, ct);

            if (existing == null)
            {
                await _dbSet.AddAsync(item, ct);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(item);
            }
        }
    }
}
