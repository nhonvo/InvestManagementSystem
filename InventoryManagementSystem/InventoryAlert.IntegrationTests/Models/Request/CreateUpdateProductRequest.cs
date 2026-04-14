namespace InventoryAlert.IntegrationTests.Models.Request;

public class CreateUpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string TickerSymbol { get; set; } = string.Empty;
    public int StockCount { get; set; }
    public decimal Price { get; set; }
    public double PriceAlertThreshold { get; set; }
    public int StockAlertThreshold { get; set; }
}
