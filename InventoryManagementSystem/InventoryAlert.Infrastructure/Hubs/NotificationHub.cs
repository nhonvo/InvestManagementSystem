using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.Infrastructure.Hubs;

/// <summary>
/// Secured SignalR hub for real-time user notifications.
/// Uses the INotificationHub interface for type-safe client calls.
/// Shared in Infrastructure so both Api and Worker can use it with the Redis backplane.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationHub>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} connected to NotificationHub (ConnectionId: {ConnectionId})", 
            userId, Context.ConnectionId);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        
        await base.OnDisconnectedAsync(exception);
    }
}
