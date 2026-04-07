using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Infrastructure.MessageConsumers;

/// <summary>
/// Core engine for processing SQS messages with deduplication, 
/// telemetry (DynamoDB), and reliable hand-off.
/// </summary>
public interface ISqsDispatcher
{
    /// <summary>
    /// Processes a single SQS message through deduplication and routing.
    /// Returns true if the message was successfully handled/enqueued and can be deleted.
    /// </summary>
    Task<bool> DispatchAsync(Message message, CancellationToken ct);

    /// <summary>
    /// Processes a batch of SQS messages.
    /// </summary>
    Task ProcessBatchAsync(IEnumerable<Message> messages, CancellationToken ct);
}
