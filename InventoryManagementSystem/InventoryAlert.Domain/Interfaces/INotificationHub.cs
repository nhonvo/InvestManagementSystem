using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

/// <summary>
/// Type-safe interface for SignalR clients to receive notifications.
/// </summary>
public interface INotificationHub
{
    /// <summary>
    /// Pushes a new notification to the connected client.
    /// </summary>
    Task ReceiveNotification(NotificationResponse notification);
}

public static class SignalRConstants
{
    public const string NotificationHubRoute = "/hubs/notifications";
}
