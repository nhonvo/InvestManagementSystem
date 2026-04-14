using System.Text.Json;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Events;
using InventoryAlert.Domain.Interfaces;

namespace InventoryAlert.Api.Services;

public class EventService(IEventPublisher eventPublisher) : IEventService
{
    private readonly IEventPublisher _eventPublisher = eventPublisher;

    private async Task PublishEnvelopeAsync<TPayload>(string eventType, TPayload payload, CancellationToken ct)
    {
        var envelope = new EventEnvelope
        {
            EventType = eventType,
            Source = "InventoryAlert.Api",
            Payload = JsonSerializer.Serialize(payload)
        };
        await _eventPublisher.PublishAsync(envelope, ct);
    }

    public async Task PublishEventAsync<TPayload>(string eventType, TPayload payload, CancellationToken ct = default)
    {
        await PublishEnvelopeAsync(eventType, payload, ct);
    }

    public Task<IEnumerable<string>> GetSupportedEventTypesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(EventTypes.All);
    }
}
