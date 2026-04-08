using System.Text.Json;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Mappings;
using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Events;
using InventoryAlert.Contracts.Events.Payloads;

namespace InventoryAlert.Api.Application.Services;

/// <summary>
/// Orchestrates: build envelope → persist EventLog → publish to SNS (via IEventPublisher).
/// Application layer — contains no infrastructure imports beyond interfaces.
/// </summary>
public class EventService(
    IEventPublisher publisher,
    IEventLogRepository eventLogRepository,
    IEventLogQuery eventLogQuery,
    ILogger<EventService> logger) : IEventService
{
    private readonly IEventPublisher _publisher = publisher;
    private readonly IEventLogRepository _eventLogRepository = eventLogRepository;
    private readonly IEventLogQuery _eventLogQuery = eventLogQuery;
    private readonly ILogger<EventService> _logger = logger;

    public async Task<IEnumerable<EventLogResponse>> GetEventLogsAsync(string eventType, int limit = 20, CancellationToken ct = default)
    {
        var logs = await _eventLogQuery.GetRecentEventsAsync(eventType, limit, ct);
        return logs.ToResponse();
    }

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

        var log = new EventLog
        {
            MessageId = envelope.CorrelationId, // using correlation ID as message ID
            EventType = eventType,
            Payload = payloadJson,
            Status = "Published",
            Source = envelope.Source,
            ProcessedAt = DateTime.UtcNow.ToString("O"),
            Ttl = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds()
        };

        await _eventLogRepository.AddAsync(log, ct);

        _logger.LogInformation("[EventService] Logged event {MessageId} to DynamoDB for {EventType}", log.MessageId, eventType);

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
