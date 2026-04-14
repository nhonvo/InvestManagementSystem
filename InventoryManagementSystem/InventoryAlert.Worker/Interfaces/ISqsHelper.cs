using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface ISqsHelper
{
    Task<List<Message>> ReceiveMessagesAsync(string queueUrl, int maxMessages = 10, CancellationToken ct = default);
    Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken ct);
}
