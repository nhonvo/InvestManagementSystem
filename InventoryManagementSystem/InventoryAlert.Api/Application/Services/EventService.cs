using System.Text.Json;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Mappings;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;

namespace InventoryAlert.Api.Application.Services;

/// <summary>
/// Orchestrates: build envelope → publish to SNS (via IEventPublisher).
/// Application layer — contains no infrastructure imports beyond interfaces.
/// </summary>
public class EventService(
    IEventPublisher publisher,
    ILogger<EventService> logger) : IEventService
{
    private readonly IEventPublisher _publisher = publisher;
    private readonly ILogger<EventService> _logger = logger;

    public async Task PublishEventAsync<TPayload>(string eventType, TPayload payload, CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var envelope = new EventEnvelope
        {
            EventType = eventType,
            Payload = payloadJson,
            CorrelationId = Guid.NewGuid().ToString(),
            Source = "InventoryAlert.Api"
        };

        await _publisher.PublishAsync(envelope, ct);
    }

    public async Task TriggerMarketAlertAsync(MarketAlertRequest request, CancellationToken ct = default)
    {
        var payload = new MarketPriceAlertPayload
        {
            ProductId = request.ProductId,
            Symbol = request.Symbol.ToUpperInvariant()
        };

        await PublishEventAsync(EventTypes.MarketPriceAlert, payload, ct);
    }

    public async Task TriggerNewsAlertAsync(NewsAlertRequest request, CancellationToken ct = default)
    {
        var payload = new CompanyNewsAlertPayload
        {
            Symbol = request.Symbol.ToUpperInvariant()
        };

        await PublishEventAsync(EventTypes.CompanyNewsAlert, payload, ct);
    }

    public Task<IEnumerable<string>> GetSupportedEventTypesAsync()
        => Task.FromResult<IEnumerable<string>>(EventTypes.All);
}
