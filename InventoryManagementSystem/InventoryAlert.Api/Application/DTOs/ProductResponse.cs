namespace InventoryAlert.Api.Application.DTOs;

/// <summary>Used for outgoing API responses. Includes Id.</summary>
public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TickerSymbol { get; set; } = string.Empty;
    public int StockCount { get; set; }
    public decimal OriginPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public double PriceAlertThreshold { get; set; }
    public int StockAlertThreshold { get; set; }
}
