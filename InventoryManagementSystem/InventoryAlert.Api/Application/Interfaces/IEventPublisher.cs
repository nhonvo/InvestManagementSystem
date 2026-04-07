using InventoryAlert.Contracts.Events;

namespace InventoryAlert.Api.Application.Interfaces;

/// <summary>
/// Publishes EventEnvelopes to the configured SNS topic.
/// Infrastructure concern: implemented in Infrastructure/Messaging.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct = default);
}
