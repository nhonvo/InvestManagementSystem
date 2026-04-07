using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface IMessageProcessor
{
    Task ProcessMessageAsync(Message message, CancellationToken cancellationToken);
}
