namespace InventoryAlert.Api.Application.DTOs
{
    /// <summary>Used for incoming Create and Update requests. Does not expose Id.</summary>
    public class ProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? TickerSymbol { get; set; }
        public int StockCount { get; set; }
        public decimal OriginPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public double PriceAlertThreshold { get; set; } // e.g. 0.1 = 10% loss triggers alert
        public int StockAlertThreshold { get; set; }    // e.g. 5 items
    }
}
