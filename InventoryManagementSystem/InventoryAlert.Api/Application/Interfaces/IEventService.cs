using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IEventService
{
    Task PublishEventAsync<TPayload>(string eventType, TPayload payload, CancellationToken ct = default);
    
    Task TriggerMarketAlertAsync(MarketAlertRequest request, CancellationToken ct = default);
    Task TriggerNewsAlertAsync(NewsAlertRequest request, CancellationToken ct = default);

    Task<IEnumerable<EventLogResponse>> GetEventLogsAsync(string eventType, int limit = 20, CancellationToken ct = default);
    Task<IEnumerable<string>> GetSupportedEventTypesAsync();
}
