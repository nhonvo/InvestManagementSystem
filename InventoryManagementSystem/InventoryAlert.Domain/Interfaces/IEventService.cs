using InventoryAlert.Domain.DTOs;

namespace InventoryAlert.Domain.Interfaces;

public interface IEventService
{
    Task PublishEventAsync<TPayload>(string eventType, TPayload payload, CancellationToken ct = default);


    Task<IEnumerable<string>> GetSupportedEventTypesAsync();
}
