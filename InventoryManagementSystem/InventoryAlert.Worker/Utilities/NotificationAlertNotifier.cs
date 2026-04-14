using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Worker.Utilities;

/// <summary>
/// This notifier currently just logs that a notification was created.
/// In a production scenario, this could also push to SignalR, Email, or SMS.
/// </summary>
public class NotificationAlertNotifier(ILogger<NotificationAlertNotifier> logger) : IAlertNotifier
{
    private readonly ILogger<NotificationAlertNotifier> _logger = logger;

    public Task NotifyAsync(Notification notification, CancellationToken ct)
    {
        _logger.LogInformation("[AlertNotifier] New internal notification for User {UserId}: {Message}",
            notification.UserId, notification.Message);

        // SignalR or external delivery logic would go here.
        return Task.CompletedTask;
    }
}
