using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IStockMetricRepository
{
    Task<StockMetric?> GetBySymbolAsync(string symbol, CancellationToken ct);
    Task UpsertAsync(StockMetric metric, CancellationToken ct);
}
