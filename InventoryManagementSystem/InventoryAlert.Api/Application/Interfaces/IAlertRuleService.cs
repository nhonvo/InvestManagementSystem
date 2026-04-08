using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IAlertRuleService
{
    Task<List<AlertRuleResponse>> GetUserAlertsAsync(string userId, CancellationToken ct = default);
    Task<AlertRuleResponse> CreateAlertAsync(string userId, AlertRuleRequest request, CancellationToken ct = default);
    Task<AlertRuleResponse> UpdateAlertAsync(string userId, Guid ruleId, AlertRuleRequest request, CancellationToken ct = default);
    Task DeleteAlertAsync(string userId, Guid ruleId, CancellationToken ct = default);
}
