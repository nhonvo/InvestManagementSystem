using Amazon.DynamoDBv2.DataModel;

namespace InventoryAlert.Contracts.Persistence.Entities;

[DynamoDBTable("inventory-event-logs")]
public class EventLogEntry
{
    [DynamoDBHashKey]
    public string EventType { get; set; } = string.Empty;

    [DynamoDBRangeKey]
    public string MessageId { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;

    public string ProcessedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public long Ttl { get; set; }
}
