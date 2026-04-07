namespace InventoryAlert.Api.Application.DTOs;

/// <summary>Returned by GetPriceLossAlertsAsync — products where price has dropped beyond the alert threshold.</summary>
public class PriceLossResponse : ProductResponse
{
    public decimal PriceDiff { get; set; }
    public decimal PriceChangePercent { get; set; }
}
