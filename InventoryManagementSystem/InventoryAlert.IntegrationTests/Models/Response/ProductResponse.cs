namespace InventoryAlert.IntegrationTests.Models.Response;

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TickerSymbol { get; set; }
    public int StockCount { get; set; }
    public decimal OriginPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public double PriceAlertThreshold { get; set; }
    public int StockAlertThreshold { get; set; }
}
