using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Mappings;

public static class EventLogMappings
{
    public static EventLogResponse ToResponse(this EventLog log) => new()
    {
        MessageId = log.MessageId,
        EventType = log.EventType,
        SourceService = log.Source,
        Payload = log.Payload,
        Status = log.Status,
        CreatedAt = DateTime.TryParse(log.ProcessedAt, out var dt) ? dt : DateTime.UtcNow
    };
    public static IEnumerable<EventLogResponse> ToResponse(this IEnumerable<EventLog> logs) => logs.Select(ToResponse);
}
