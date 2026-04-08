using System.Text.Json;
using InventoryAlert.Contracts.Common.Exceptions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Events;

namespace InventoryAlert.Api.Application.Services;

public class AlertRuleService(
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<AlertRuleService> logger) : IAlertRuleService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEventPublisher _events = eventPublisher;
    private readonly ILogger<AlertRuleService> _logger = logger;

    public async Task<List<AlertRuleResponse>> GetUserAlertsAsync(string userId, CancellationToken ct = default)
    {
        var alerts = await _unitOfWork.AlertRules.GetByUserIdAsync(userId, ct);
        return [.. alerts.Select(MapToResponse)];
    }

    public async Task<AlertRuleResponse> CreateAlertAsync(string userId, AlertRuleRequest request, CancellationToken ct = default)
    {
        var symbol = request.Symbol.ToUpperInvariant();

        // 1. Verify if user has subscribed to the symbol
        var subscribed = await _unitOfWork.Watchlists.ExistsAsync(userId, symbol, ct);

        if (!subscribed)
        {
            throw new UserFriendlyException(ErrorCode.BadRequest, $"You must add '{symbol}' to your watchlist before creating an alert.");
        }

        AlertRuleResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var rule = new AlertRule
            {
                UserId = userId,
                Symbol = symbol,
                Field = request.Field,
                Operator = request.Operator,
                Threshold = request.Threshold,
                NotifyChannel = request.NotifyChannel
            };

            await _unitOfWork.AlertRules.AddAsync(rule, ct);

            await _events.PublishAsync(new EventEnvelope
            {
                EventType = EventTypes.AlertRuleCreated,
                Source = "InventoryAlert.Api",
                Payload = JsonSerializer.Serialize(new { RuleId = rule.Id, Symbol = rule.Symbol })
            }, ct);

            _logger.LogInformation("[AlertRuleService] Created rule {RuleId} for {Symbol}.", rule.Id, rule.Symbol);
            result = MapToResponse(rule);
        }, ct);
        return result;
    }

    public async Task<AlertRuleResponse> UpdateAlertAsync(string userId, Guid ruleId, AlertRuleRequest request, CancellationToken ct = default)
    {
        AlertRuleResponse result = null!;
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var rule = await _unitOfWork.AlertRules.GetByIdAsync(ruleId, ct)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, $"Alert rule {ruleId} not found.");

            if (rule.UserId != userId)
            {
                throw new UserFriendlyException(ErrorCode.Forbidden, "You do not have permission to modify this alert.");
            }

            rule.Field = request.Field;
            rule.Operator = request.Operator;
            rule.Threshold = request.Threshold;
            rule.NotifyChannel = request.NotifyChannel;

            await _unitOfWork.AlertRules.UpdateAsync(rule);

            await _events.PublishAsync(new EventEnvelope
            {
                EventType = EventTypes.AlertRuleUpdated,
                Source = "InventoryAlert.Api",
                Payload = JsonSerializer.Serialize(new { RuleId = rule.Id, Symbol = rule.Symbol })
            }, ct);

            _logger.LogInformation("[AlertRuleService] Updated rule {RuleId}.", ruleId);
            result = MapToResponse(rule);
        }, ct);
        return result;
    }

    public async Task DeleteAlertAsync(string userId, Guid ruleId, CancellationToken ct = default)
    {
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            var rule = await _unitOfWork.AlertRules.GetByIdAsync(ruleId, ct)
                ?? throw new UserFriendlyException(ErrorCode.NotFound, $"Alert rule {ruleId} not found.");

            if (rule.UserId != userId)
            {
                throw new UserFriendlyException(ErrorCode.Forbidden, "You do not have permission to delete this alert.");
            }

            await _unitOfWork.AlertRules.DeleteAsync(rule);

            await _events.PublishAsync(new EventEnvelope
            {
                EventType = EventTypes.AlertRuleDeleted,
                Source = "InventoryAlert.Api",
                Payload = JsonSerializer.Serialize(new { RuleId = rule.Id, Symbol = rule.Symbol })
            }, ct);

            _logger.LogInformation("[AlertRuleService] Deleted rule {RuleId}.", ruleId);
        }, ct);
    }

    private static AlertRuleResponse MapToResponse(AlertRule a) => new(
        a.Id, a.UserId, a.Symbol, a.Field, a.Operator,
        a.Threshold, a.NotifyChannel, a.IsActive,
        a.LastTriggeredAt, a.CreatedAt);
}
