using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface IProcessQueueJob
{
    Task ExecuteAsync(CancellationToken ct);
    Task ProcessBatchAsync(IEnumerable<Message> messages, CancellationToken ct);
}

