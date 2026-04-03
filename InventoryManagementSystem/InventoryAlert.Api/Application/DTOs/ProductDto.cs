namespace InventoryAlert.Api.Application.DTOs
{
    public class ProductRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? TickerSymbol { get; set; }
        public int StockCount { get; set; }
        public decimal OriginPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public double PriceAlertThreshold { get; set; } // e.g., 0.1 for 10%
        public int StockAlertThreshold { get; set; }    // e.g., 5 items
    }
    // 2. Used for OUTGOING Responses (Includes ID)
    public class ProductDto : ProductRequestDto
    {
        public int Id { get; set; }
    }
    // 3. For the finnhub results
    public class ProductLossDto : ProductDto
    {
        public decimal PriceDiff { get; set; }
        public decimal PriceChangePercent { get; set; }
    }
}
