using Amazon.SQS.Model;
using InventoryAlert.Domain.Events;

namespace InventoryAlert.Worker.Interfaces;

public interface IIntegrationMessageRouter
{
    Task ProcessMessageAsync(Message message, CancellationToken ct);
    Task<bool> ProcessAndAcknowledgeAsync(Message message, CancellationToken ct);
    Task<bool> RouteEnvelopeAsync(EventEnvelope envelope, CancellationToken ct);
}
