using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IPriceHistoryRepository : IGenericRepository<PriceHistory>
{
    Task<IEnumerable<PriceHistory>> GetBySymbolAsync(string symbol, int limit, CancellationToken ct);

    /// <summary>
    /// Deletes records older than the specified cutoff date.
    /// Implementation should use batched deletes to avoid lock contention.
    /// </summary>
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct);
}
