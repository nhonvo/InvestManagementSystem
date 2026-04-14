using InventoryAlert.IntegrationTests.Models.Request;
using InventoryAlert.IntegrationTests.Models.Response;

namespace InventoryAlert.IntegrationTests.Helpers;

public static class ProductHelpers
{
    public static CreateUpdateProductRequest CreateValidProductRequest()
    {
        return new CreateUpdateProductRequest
        {
            Name = "Test Product",
            TickerSymbol = "UPOW",
            StockCount = 100,
            Price = 10.0m,
            PriceAlertThreshold = 0.2,
            StockAlertThreshold = 20
        };
    }

    public static CreateUpdateProductRequest CreateValidUpdateProductRequest()
    {
        return new CreateUpdateProductRequest
        {
            Name = "Updated Product",
            TickerSymbol = "UPOW",
            StockCount = 150,
            Price = 700.0m,
            PriceAlertThreshold = 0.25,
            StockAlertThreshold = 15
        };
    }

    public static ProductResponse CreateExpectedProductResponse(CreateUpdateProductRequest createRequest, int id = 1)
    {
        return new ProductResponse
        {
            Id = id,
            Name = createRequest.Name,
            TickerSymbol = createRequest.TickerSymbol,
            StockCount = createRequest.StockCount,
            OriginPrice = createRequest.Price,
            CurrentPrice = createRequest.Price,
            PriceAlertThreshold = createRequest.PriceAlertThreshold,
            StockAlertThreshold = createRequest.StockAlertThreshold
        };
    }
}
