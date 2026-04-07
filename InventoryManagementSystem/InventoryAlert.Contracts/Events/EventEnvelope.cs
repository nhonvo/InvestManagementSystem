namespace InventoryAlert.Contracts.Events;

/// <summary>
/// Standard envelope for all events flowing through SNS → SQS.
/// Both the Api (publisher) and Worker (consumer) share this record.
///
/// Field naming uses the SNS raw-message-delivery convention:
///   EventType — the canonical event name (see EventTypes constants)
///   Payload   — JSON-serialized inner payload object
/// </summary>
public record EventEnvelope
{
    /// <summary>Unique identifier for this event instance. Used for deduplication.</summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Canonical event type. Must match an EventTypes constant.</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>Source service that emitted the event (e.g. "InventoryAlert.Api").</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>JSON-serialized inner payload. Deserialize based on EventType.</summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the event was created.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Optional trace/correlation identifier for cross-service logging.</summary>
    public string CorrelationId { get; init; } = string.Empty;
}
