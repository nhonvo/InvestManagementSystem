using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Api.Services;

public class AlertRuleService(IUnitOfWork unitOfWork, IStockDataService stockDataService) : IAlertRuleService
{
    public async Task<IEnumerable<AlertRuleResponse>> GetByUserIdAsync(string userId, CancellationToken ct)
    {
        var rules = await unitOfWork.AlertRules.GetByUserIdAsync(userId, ct);
        return rules.Select(MapToResponse);
    }

    public async Task<AlertRuleResponse> CreateAsync(AlertRuleRequest request, string userId, CancellationToken ct)
    {
        var userGuid = Guid.Parse(userId);

        var normalizedSymbol = request.TickerSymbol?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSymbol))
            throw new InvalidOperationException("TickerSymbol is required.");

        if (request.TargetValue <= 0)
            throw new InvalidOperationException("TargetValue must be greater than 0.");

        // Discovery flow: auto-resolve and persist symbol metadata if missing.
        // This removes the UI/API coupling that required users to 'visit a quote/profile' before creating alerts.
        _ = await stockDataService.GetProfileAsync(normalizedSymbol, ct) ?? throw new InvalidOperationException($"Symbol {normalizedSymbol} could not be resolved.");
        var rule = new AlertRule
        {
            UserId = userGuid,
            TickerSymbol = normalizedSymbol,
            Condition = request.Condition,
            TargetValue = request.TargetValue,
            TriggerOnce = request.TriggerOnce,
            IsActive = true
        };

        await unitOfWork.AlertRules.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(rule);
    }

    public async Task<AlertRuleResponse> UpdateAsync(Guid id, AlertRuleRequest request, string userId, CancellationToken ct)
    {
        var rule = await unitOfWork.AlertRules.GetByIdAsync(id, ct);
        if (rule == null || rule.UserId != Guid.Parse(userId))
        {
            throw new KeyNotFoundException("Alert rule not found.");
        }

        // Full replacement — all fields overwritten
        rule.TickerSymbol = request.TickerSymbol;
        rule.Condition = request.Condition;
        rule.TargetValue = request.TargetValue;
        rule.TriggerOnce = request.TriggerOnce;

        await unitOfWork.SaveChangesAsync(ct);
        return MapToResponse(rule);
    }

    public async Task<AlertRuleResponse> ToggleAsync(Guid id, bool isActive, string userId, CancellationToken ct)
    {
        var rule = await unitOfWork.AlertRules.GetByIdAsync(id, ct);
        if (rule == null || rule.UserId != Guid.Parse(userId))
        {
            throw new KeyNotFoundException("Alert rule not found.");
        }

        rule.IsActive = isActive;
        await unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(rule);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct)
    {
        var rule = await unitOfWork.AlertRules.GetByIdAsync(id, ct);
        if (rule != null && rule.UserId == Guid.Parse(userId))
        {
            await unitOfWork.AlertRules.DeleteAsync(rule, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return true;
        }
        return false;
    }

    private static AlertRuleResponse MapToResponse(AlertRule rule) =>
        new(rule.Id, rule.TickerSymbol, rule.Condition, rule.TargetValue,
            rule.IsActive, rule.TriggerOnce, rule.LastTriggeredAt);
}
