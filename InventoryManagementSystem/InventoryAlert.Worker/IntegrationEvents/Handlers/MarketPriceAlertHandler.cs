using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Worker.IntegrationEvents.Handlers;

public class MarketPriceAlertHandler(
    IUnitOfWork unitOfWork,
    IAlertNotifier notifier,
    ILogger<MarketPriceAlertHandler> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAlertNotifier _notifier = notifier;
    private readonly ILogger<MarketPriceAlertHandler> _logger = logger;

    public async Task HandleAsync(MarketPriceAlertPayload payload, CancellationToken ct)
    {
        _logger.LogInformation("[MarketPriceAlertHandler] Processing price alert for {Symbol} at {NewPrice}", payload.Symbol, payload.NewPrice);

        var allRules = await _unitOfWork.AlertRules.GetBySymbolAsync(payload.Symbol, ct);
        var activeRules = allRules.Where(r => r.IsActive).ToList();
        if (activeRules.Count == 0) return;

        // Perform evaluation before opening a transaction to improve performance
        var triggeredRules = activeRules.Where(rule => IsTriggered(rule, payload.NewPrice)).ToList();
        if (triggeredRules.Count == 0)
        {
            _logger.LogInformation("[MarketPriceAlertHandler] No rules triggered for {Symbol} at {Price}.", payload.Symbol, payload.NewPrice);
            return;
        }

        var triggeredNotifications = new List<Notification>();

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            foreach (var rule in triggeredRules)
            {
                var notification = new Notification
                {
                    UserId = rule.UserId,
                    AlertRuleId = rule.Id,
                    TickerSymbol = payload.Symbol,
                    Message = $"Price alert: {payload.Symbol} reached {payload.NewPrice:C} (Rule: {rule.Condition} {rule.TargetValue:C})",
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

            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

        foreach (var note in triggeredNotifications)
        {
            await _notifier.NotifyAsync(note, ct);
            _logger.LogInformation("[MarketPriceAlertHandler] Dispatched real-time notification for {Symbol} to User {UserId}", note.TickerSymbol, note.UserId);
        }
    }

    private static bool IsTriggered(AlertRule rule, decimal price) => rule.Condition switch
    {
        AlertCondition.PriceAbove => price > rule.TargetValue,
        AlertCondition.PriceBelow => price < rule.TargetValue,
        AlertCondition.PriceTargetReached => Math.Abs(price - rule.TargetValue) < 0.01m,
        _ => false
    };
}
