namespace InventoryAlert.Worker.Application.IntegrationHandlers;

/// <summary>
/// Fallback handler invoked when an SQS message carries an EventType not registered
/// in the application. Logs a structured warning instead of silently discarding the message.
/// Maps to the "Unknown" branch of the dispatcher — always deletes the message from SQS
/// after logging to avoid it blocking the queue indefinitely.
/// </summary>
public sealed class UnknownEventHandler(ILogger<UnknownEventHandler> logger)
{
    private readonly ILogger<UnknownEventHandler> _logger = logger;

    public Task HandleAsync(string eventType, string rawMessageBody, string messageId, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[UnknownEventHandler] Received unregistered EventType={EventType} | MessageId={MessageId}. " +
            "Message will be acknowledged and discarded. Register a handler to process this type.",
            eventType,
            messageId);

        return Task.CompletedTask;
    }
}
