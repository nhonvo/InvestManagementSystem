namespace InventoryAlert.Api.Application.Interfaces;

/// <summary>Application-layer interface for alert notifications (Telegram, email, etc.).</summary>
public interface IAlertNotifier
{
    Task NotifyAsync(string message, CancellationToken ct = default);
}
