namespace InventoryAlert.Domain.Interfaces;

/// <summary>
/// Specialized service for direct SQS messaging using the "Price Changed" event model.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Enqueues a notification that a ticker's price has changed, triggering a multi-user alert evaluation.
    /// </summary>
    Task EnqueueAlertEvaluationAsync(string symbol, decimal price, CancellationToken ct = default);

    /// <summary>
    /// Generic method to send a raw message to a specific queue.
    /// </summary>
    Task SendMessageAsync(string queueName, string messageBody, CancellationToken ct = default);
}
