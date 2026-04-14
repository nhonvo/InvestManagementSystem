using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IAlertNotifier
{
    /// <summary>
    /// Delivers a notification to the user (e.g. via SignalR, Email, or just persistence).
    /// </summary>
    Task NotifyAsync(Notification notification, CancellationToken ct);
}
