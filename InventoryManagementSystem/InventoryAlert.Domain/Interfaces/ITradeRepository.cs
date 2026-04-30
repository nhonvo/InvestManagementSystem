using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface ITradeRepository : IGenericRepository<Trade>
{
    Task<IEnumerable<Trade>> GetByUserAndSymbolAsync(Guid userId, string symbol, CancellationToken ct);
    Task<IEnumerable<Trade>> GetByUserAndSymbolsAsync(Guid userId, IEnumerable<string> symbols, CancellationToken ct);

    /// <summary>
    /// Computes net holdings via SUM(Buy) - SUM(Sell).
    /// </summary>
    Task<decimal> GetNetHoldingsAsync(Guid userId, string symbol, CancellationToken ct);
}
