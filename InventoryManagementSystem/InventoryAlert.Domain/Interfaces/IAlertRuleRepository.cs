using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IAlertRuleRepository : IGenericRepository<AlertRule>
{
    Task<IEnumerable<AlertRule>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IEnumerable<AlertRule>> GetBySymbolAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<AlertRule>> GetBySymbolsAsync(IEnumerable<string> symbols, CancellationToken ct = default);
}
