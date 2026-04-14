using Amazon.SQS.Model;

namespace InventoryAlert.Worker.Interfaces;

public interface IIntegrationMessageRouter
{
    Task ProcessMessageAsync(Message message, CancellationToken ct);
}

