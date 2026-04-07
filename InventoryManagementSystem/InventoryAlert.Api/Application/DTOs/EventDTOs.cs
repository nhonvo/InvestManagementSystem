namespace InventoryAlert.Api.Application.DTOs;

/// <summary>Generic event publish request — any event type with freeform payload.</summary>
public class PublishEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public object Payload { get; set; } = new();
}

/// <summary>Request body for manually triggering a MarketPriceAlert check.</summary>
public class MarketAlertRequest
{
    public int ProductId { get; set; }
    public string Symbol { get; set; } = string.Empty;
}

/// <summary>Request body for manually triggering a CompanyNews sync.</summary>
public class NewsAlertRequest
{
    public string Symbol { get; set; } = string.Empty;
}
