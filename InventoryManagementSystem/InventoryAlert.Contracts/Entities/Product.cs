namespace InventoryAlert.Contracts.Entities;

/// <summary>
/// The core inventory item. Shared between API, Worker, and any future service.
/// Navigation property <see cref="LastAlertSentAt"/> is used by the API alert cooldown logic.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TickerSymbol { get; set; } = string.Empty;
    public decimal OriginPrice { get; set; }
    public decimal CurrentPrice { get; set; }  // Last synced price from Finnhub
    public double PriceAlertThreshold { get; set; } // e.g. 0.2 = alert when drop > 20%
    public int StockAlertThreshold { get; set; } // Alert when stock counts <= this
    public int StockCount { get; set; }
    public DateTime? LastAlertSentAt { get; set; }
}
