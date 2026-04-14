using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class StockMetricRepository(AppDbContext context) : IStockMetricRepository
{
    private readonly DbSet<StockMetric> _metrics = context.StockMetrics;

    public async Task<StockMetric?> GetBySymbolAsync(string symbol, CancellationToken ct)
    {
        return await _metrics.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TickerSymbol == symbol, ct);
    }

    public async Task UpsertAsync(StockMetric metric, CancellationToken ct)
    {
        var existing = await _metrics.FindAsync([metric.TickerSymbol], cancellationToken: ct);

        if (existing == null)
        {
            await _metrics.AddAsync(metric, ct);
        }
        else
        {
            context.Entry(existing).CurrentValues.SetValues(metric);
        }
    }
}
