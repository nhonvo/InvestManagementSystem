using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface ISqsHelper
{
    /// <summary>Pulls a batch of messages using Long Polling.</summary>
    Task<List<Message>> ReceiveMessagesAsync(string queueUrl, int maxMessages = 10, CancellationToken ct = default);
    /// <summary>Removes a message from the queue after successful processing.</summary>
    Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken ct = default);
    /// <summary>Moves a failed message to the Dead Letter Queue (DLQ).</summary>
    Task MoveToDlqAsync(Message message, string dlqUrl, CancellationToken ct = default);
    /// <summary>Extracts the 'ApproximateReceiveCount' attribute safely.</summary>
    int GetReceiveCount(Message message);
}
