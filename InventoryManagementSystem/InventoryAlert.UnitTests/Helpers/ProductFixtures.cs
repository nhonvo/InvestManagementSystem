using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.UnitTests.Helpers;

public static class ProductFixtures
{
    private static int _counter = 1000;

    public static Product BuildProduct(
        int? id = null,
        string name = "TestProduct",
        string? ticker = null,
        decimal originPrice = 100m,
        decimal currentPrice = 90m,
        double threshold = 0.2,
        int stock = 10,
        int stockThreshold = 10)
    {
        var nextId = id ?? Interlocked.Increment(ref _counter);
        return new()
        {
            Id = nextId,
            Name = name,
            TickerSymbol = ticker ?? $"T{nextId}",
            OriginPrice = originPrice,
            CurrentPrice = currentPrice,
            PriceAlertThreshold = threshold,
            StockAlertThreshold = stockThreshold,
            StockCount = stock
        };
    }

    public static ProductRequest BuildRequest(
        string name = "TestProduct",
        string? ticker = "TST",
        decimal price = 100m,
        double threshold = 0.2,
        int stock = 10,
        int stockThreshold = 10) => new()
        {
            Name = name,
            TickerSymbol = ticker ?? string.Empty,
            Price = price,
            PriceAlertThreshold = threshold,
            StockAlertThreshold = stockThreshold,
            StockCount = stock
        };

    public static ProductResponse BuildResponse(
        int id = 1,
        string name = "TestProduct",
        string ticker = "TST",
        decimal originPrice = 100m,
        decimal currentPrice = 90m,
        double threshold = 0.2,
        int stock = 10,
        int stockThreshold = 10) => new()
        {
            Id = id,
            Name = name,
            TickerSymbol = ticker,
            OriginPrice = originPrice,
            CurrentPrice = currentPrice,
            PriceAlertThreshold = threshold,
            StockAlertThreshold = stockThreshold,
            StockCount = stock
        };

    public static FinnhubQuoteResponse BuildQuote(decimal currentPrice = 90m) => new()
    {
        CurrentPrice = currentPrice
    };
}
