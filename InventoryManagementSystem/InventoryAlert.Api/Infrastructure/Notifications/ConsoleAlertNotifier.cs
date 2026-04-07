using InventoryAlert.Api.Application.Interfaces;

namespace InventoryAlert.Api.Infrastructure.Notifications;

/// <summary>
/// Log-only alert notifier used until Telegram integration is wired.
/// Replace with TelegramAlertNotifier when Phase F is implemented.
/// </summary>
public sealed class ConsoleAlertNotifier(ILogger<ConsoleAlertNotifier> logger) : IAlertNotifier
{
    private readonly ILogger<ConsoleAlertNotifier> _logger = logger;

    public Task NotifyAsync(string message, CancellationToken ct = default)
    {
        _logger.LogWarning("[ALERT] {Message}", message);
        return Task.CompletedTask;
    }
}
