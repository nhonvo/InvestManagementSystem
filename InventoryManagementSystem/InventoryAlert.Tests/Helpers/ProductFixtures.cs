using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Domain.Entities;
using InventoryAlert.Api.Infrastructure.External.Models;

namespace InventoryAlert.Tests.Helpers;

public static class ProductFixtures
{
    public static Product BuildProduct(
        int id = 1,
        string name = "TestProduct",
        string ticker = "TST",
        decimal originPrice = 100m,
        decimal currentPrice = 90m,
        double threshold = 0.2,
        int stock = 10) => new()
    {
        Id = id,
        Name = name,
        TickerSymbol = ticker,
        OriginPrice = originPrice,
        CurrentPrice = currentPrice,
        PriceAlertThreshold = threshold,
        StockCount = stock
    };

    public static ProductRequest BuildRequest(
        string name = "TestProduct",
        string? ticker = "TST",
        decimal originPrice = 100m,
        decimal currentPrice = 90m,
        double threshold = 0.2,
        int stock = 10,
        int stockAlertThreshold = 0) => new()
    {
        Name = name,
        TickerSymbol = ticker,
        OriginPrice = originPrice,
        CurrentPrice = currentPrice,
        PriceAlertThreshold = threshold,
        StockCount = stock,
        StockAlertThreshold = stockAlertThreshold
    };

    public static ProductResponse BuildResponse(
        int id = 1,
        string name = "TestProduct",
        string ticker = "TST",
        decimal originPrice = 100m,
        decimal currentPrice = 90m,
        double threshold = 0.2,
        int stock = 10) => new()
    {
        Id = id,
        Name = name,
        TickerSymbol = ticker,
        OriginPrice = originPrice,
        CurrentPrice = currentPrice,
        PriceAlertThreshold = threshold,
        StockCount = stock
    };

    public static FinnhubQuoteResponse BuildQuote(decimal currentPrice = 90m) => new()
    {
        CurrentPrice = currentPrice
    };
}
