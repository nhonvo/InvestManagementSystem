using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IEarningsSurpriseRepository
{
    Task<IEnumerable<EarningsSurprise>> GetBySymbolAsync(string symbol, CancellationToken ct);
    Task UpsertRangeAsync(IEnumerable<EarningsSurprise> earnings, CancellationToken ct);
}
