using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Api.Domain.Interfaces;

public interface IAlertRuleRepository : IGenericRepository<AlertRule>
{
    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AlertRule>> GetByUserIdAsync(string userId, CancellationToken ct = default);
}
