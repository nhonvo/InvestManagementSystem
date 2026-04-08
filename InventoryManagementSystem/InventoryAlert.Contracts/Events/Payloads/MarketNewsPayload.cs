namespace InventoryAlert.Contracts.Events.Payloads;

public record MarketNewsPayload(
    string Category,
    string PublishedAt,
    string Headline,
    string Summary,
    string Source,
    string Url,
    string ImageUrl,
    long FinnhubId);
