using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IInsiderTransactionRepository
{
    Task<IEnumerable<InsiderTransaction>> GetBySymbolAsync(string symbol, CancellationToken ct);

    /// <summary>
    /// Replaces all insider transactions for a symbol with a new set.
    /// Implementation should delete and bulk insert.
    /// </summary>
    Task ReplaceForSymbolAsync(string symbol, IEnumerable<InsiderTransaction> entries, CancellationToken ct);
}
