namespace InventoryAlert.Api.Domain.Interfaces;

/// <summary>Repository contract for EventLog audit trail persistence.</summary>
public interface IEventLogRepository
{
    Task<EventLog> AddAsync(EventLog log, CancellationToken ct);
    Task<IEnumerable<EventLog>> GetAllAsync(CancellationToken ct);
}
