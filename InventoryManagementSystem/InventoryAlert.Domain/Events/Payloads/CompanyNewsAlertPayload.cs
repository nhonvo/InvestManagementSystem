namespace InventoryAlert.Domain.Events.Payloads;

/// <summary>
/// Trigger payload: indicates a symbol needs a news headlines sync.
/// No longer carries the article details.
/// </summary>
public record CompanyNewsAlertPayload
{
    public string Symbol { get; init; } = string.Empty;
}
