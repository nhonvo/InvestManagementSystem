using InventoryAlert.Domain.Events;

namespace InventoryAlert.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken ct = default);
}

