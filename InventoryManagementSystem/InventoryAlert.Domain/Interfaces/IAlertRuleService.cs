using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface IAlertRuleService
{
    Task<IEnumerable<AlertRuleResponse>> GetByUserIdAsync(string userId, CancellationToken ct);
    Task<AlertRuleResponse> CreateAsync(AlertRuleRequest request, string userId, CancellationToken ct);
    Task<AlertRuleResponse> UpdateAsync(Guid id, AlertRuleRequest request, string userId, CancellationToken ct);
    Task<AlertRuleResponse> ToggleAsync(Guid id, bool isActive, string userId, CancellationToken ct);
    Task DeleteAsync(Guid id, string userId, CancellationToken ct);
}

