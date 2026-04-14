using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Api.Services;

public class AlertRuleService(IUnitOfWork unitOfWork) : IAlertRuleService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<AlertRuleResponse>> GetByUserIdAsync(string userId, CancellationToken ct)
    {
        var rules = await _unitOfWork.AlertRules.GetByUserIdAsync(userId, ct);
        return rules.Select(MapToResponse);
    }

    public async Task<AlertRuleResponse> CreateAsync(AlertRuleRequest request, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);

        // Discovery flow: Ensure symbol metadata exists
        var listing = await _unitOfWork.StockListings.FindBySymbolAsync(request.TickerSymbol, ct);
        if (listing == null)
        {
            throw new InvalidOperationException($"Symbol {request.TickerSymbol} must be resolved (visited at least once) before creating alerts.");
        }

        var rule = new AlertRule
        {
            UserId = userGuid,
            TickerSymbol = request.TickerSymbol,
            Condition = request.Condition,
            TargetValue = request.TargetValue,
            TriggerOnce = request.TriggerOnce,
            IsActive = true
        };

        await _unitOfWork.AlertRules.AddAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(rule);
    }

    public async Task<AlertRuleResponse> UpdateAsync(Guid id, AlertRuleRequest request, string userId, CancellationToken ct)
    {
        var rule = await _unitOfWork.AlertRules.GetByIdAsync(id, ct);
        if (rule == null || rule.UserId != Guid.Parse(userId))
        {
            throw new KeyNotFoundException("Alert rule not found.");
        }

        // Full replacement — all fields overwritten
        rule.TickerSymbol = request.TickerSymbol;
        rule.Condition = request.Condition;
        rule.TargetValue = request.TargetValue;
        rule.TriggerOnce = request.TriggerOnce;

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToResponse(rule);
    }

    public async Task<AlertRuleResponse> ToggleAsync(Guid id, bool isActive, string userId, CancellationToken ct)
    {
        var rule = await _unitOfWork.AlertRules.GetByIdAsync(id, ct);
        if (rule == null || rule.UserId != Guid.Parse(userId))
        {
            throw new KeyNotFoundException("Alert rule not found.");
        }

        rule.IsActive = isActive;
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(rule);
    }

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct)
    {
        var rule = await _unitOfWork.AlertRules.GetByIdAsync(id, ct);
        if (rule != null && rule.UserId == Guid.Parse(userId))
        {
            await _unitOfWork.AlertRules.DeleteAsync(rule, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    private static AlertRuleResponse MapToResponse(AlertRule rule) =>
        new(rule.Id, rule.TickerSymbol, rule.Condition, rule.TargetValue,
            rule.IsActive, rule.TriggerOnce, rule.LastTriggeredAt);
}

