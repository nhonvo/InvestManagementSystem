namespace InventoryAlert.Api.Application.Interfaces;

public interface IEventLogQuery
{
    Task<IEnumerable<EventLog>> GetRecentEventsAsync(string eventType, int limit = 20, CancellationToken ct = default);
}
