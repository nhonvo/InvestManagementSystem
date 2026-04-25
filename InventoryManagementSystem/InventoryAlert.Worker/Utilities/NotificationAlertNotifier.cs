using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using InventoryAlert.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Worker.Utilities;

/// <summary>
/// Refactored notifier that pushes real-time alerts via SignalR (Redis Backplane).
/// </summary>
public class NotificationAlertNotifier(
    IHubContext<NotificationHub, INotificationHub> hubContext,
    ILogger<NotificationAlertNotifier> logger) : IAlertNotifier
{
    private readonly IHubContext<NotificationHub, INotificationHub> _hubContext = hubContext;
    private readonly ILogger<NotificationAlertNotifier> _logger = logger;

    public async Task NotifyAsync(Notification notification, CancellationToken ct)
    {
        _logger.LogInformation("[AlertNotifier] New internal notification for User {UserId}: {Message}",
            notification.UserId, notification.Message);

        try
        {
            // Map to response DTO for consistent client consumption
            var dto = new NotificationResponse(
                notification.Id,
                notification.Message,
                notification.TickerSymbol ?? "MARKET",
                notification.IsRead,
                notification.CreatedAt
            );

            // Push to specific user via SignalR
            // The Redis backplane ensures this reaches the correct Api instance where the user is connected.
            await _hubContext.Clients.User(notification.UserId.ToString())
                .ReceiveNotification(dto);
            
            _logger.LogInformation("[AlertNotifier] Successfully pushed SignalR alert to User {UserId}", notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AlertNotifier] Failed to push SignalR notification for User {UserId}", notification.UserId);
            // We don't throw here to avoid failing the sync job; the persistent notification is already in DB.
        }
    }
}
