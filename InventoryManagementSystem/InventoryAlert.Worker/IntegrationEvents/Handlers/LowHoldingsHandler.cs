using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Events.Payloads;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Worker.IntegrationEvents.Handlers;

public class LowHoldingsHandler(
    IUnitOfWork unitOfWork,
    IAlertNotifier notifier,
    ILogger<LowHoldingsHandler> logger)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAlertNotifier _notifier = notifier;
    private readonly ILogger<LowHoldingsHandler> _logger = logger;

    public async Task HandleAsync(LowHoldingsAlertPayload payload, CancellationToken ct)
    {
        _logger.LogInformation("[LowHoldingsHandler] Processing alert for {Symbol} (User: {UserId})",
            payload.TickerSymbol, payload.UserId);

        var notification = new Notification
        {
            UserId = payload.UserId,
            Message = $"Low holdings alert: Your {payload.TickerSymbol} balance has reached {payload.CurrentQuantity}, which is at or below your threshold of {payload.Threshold}.",
            TickerSymbol = payload.TickerSymbol,
            IsRead = false
        };

        await _unitOfWork.Notifications.AddAsync(notification, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _notifier.NotifyAsync(notification, ct);
    }
}
