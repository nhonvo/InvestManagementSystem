using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface IMessageProcessor
{
    /// <summary>
    /// Processes a message and returns true if SQS should delete it (ACK), false to redeliver.
    /// </summary>
    Task<bool> ProcessAndAcknowledgeAsync(Message message, CancellationToken cancellationToken);

    /// <summary>Convenience wrapper — delegates to ProcessAndAcknowledgeAsync.</summary>
    Task ProcessMessageAsync(Message message, CancellationToken cancellationToken);
}
