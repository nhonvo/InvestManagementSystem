namespace InventoryAlert.Api.Application.Interfaces;

/// <summary>
/// Orchestrates event publishing: builds the EventEnvelope, persists the EventLog, then publishes to SNS.
/// </summary>
public interface IEventService
{
    Task PublishEventAsync<TPayload>(string eventType, TPayload payload, CancellationToken ct = default);
    Task<IEnumerable<string>> GetSupportedEventTypesAsync();
    Task<IEnumerable<InventoryAlert.Api.Application.DTOs.EventLogResponse>> GetEventLogsAsync(string eventType, int limit = 20, CancellationToken ct = default);
}
