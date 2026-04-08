using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface IAlertRuleRepository : IGenericRepository<AlertRule>
{
    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AlertRule>> GetByUserIdAsync(string userId, CancellationToken ct = default);
}
