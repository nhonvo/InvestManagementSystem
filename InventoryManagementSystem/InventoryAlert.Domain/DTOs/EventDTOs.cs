namespace InventoryAlert.Domain.DTOs;

/// <summary>Generic event publish request — any event type with freeform payload.</summary>
public class PublishEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public object Payload { get; set; } = new();
}

