namespace InventoryAlert.Api.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TickerSymbol { get; set; } = string.Empty;
        public decimal OriginPrice { get; set; }  
        public decimal CurrentPrice { get; set; } // Last synced price from Finnhub

        public double PriceAlertThreshold { get; set; } // Alert when Change > this % (e.g. 0.2)

        public int StockCount { get; set; }
        public DateTime? LastAlertSentAt { get; set; }
    }
}
