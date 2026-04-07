namespace InventoryAlert.Api.Application.DTOs;

public class EventLogResponse
{
    public string MessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string SourceService { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
