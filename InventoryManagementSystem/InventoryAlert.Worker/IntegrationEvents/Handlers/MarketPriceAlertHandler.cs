using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Worker.IntegrationEvents.Handlers;

public class MarketPriceAlertHandler(
    IUnitOfWork unitOfWork,
    IAlertNotifier notifier,
    IAlertRuleEvaluator evaluator,
    ILogger<MarketPriceAlertHandler> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAlertNotifier _notifier = notifier;
    private readonly IAlertRuleEvaluator _evaluator = evaluator;
    private readonly ILogger<MarketPriceAlertHandler> _logger = logger;

    public async Task HandleAsync(MarketPriceAlertPayload payload, CancellationToken ct)
    {
        _logger.LogInformation("[MarketPriceAlertHandler] Processing price alert for {Symbol} at {NewPrice}", payload.Symbol, payload.NewPrice);

        var allRules = await _unitOfWork.AlertRules.GetBySymbolAsync(payload.Symbol, ct);
        var activeRules = allRules.Where(r => r.IsActive).ToList();
        if (activeRules.Count == 0) return;

        var triggeredNotifications = new List<Notification>();

        // Re-use evaluator logic for consistency + cooldown support
        foreach (var rule in activeRules)
        {
            var (isBreached, message) = await _evaluator.EvaluateAsync(rule, payload.NewPrice, ct);
            if (!isBreached) continue;

            var notification = new Notification
            {
                UserId = rule.UserId,
                AlertRuleId = rule.Id,
                TickerSymbol = payload.Symbol,
                Message = message,
                Type = NotificationType.Price,
                Severity = NotificationSeverity.Warning,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification, ct);

            rule.LastTriggeredAt = DateTime.UtcNow;
            if (rule.TriggerOnce)
            {
                rule.IsActive = false;
            }

            await _unitOfWork.AlertRules.UpdateAsync(rule, ct);
            triggeredNotifications.Add(notification);
        }

        if (triggeredNotifications.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);

            foreach (var note in triggeredNotifications)
            {
                await _notifier.NotifyAsync(note, ct);
                _logger.LogInformation("[MarketPriceAlertHandler] Dispatched real-time notification for {Symbol} to User {UserId}", note.TickerSymbol, note.UserId);
            }
        }
    }
}
