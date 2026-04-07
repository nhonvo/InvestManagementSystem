using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Mappings;

public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product)
    {
        if (product is null) return null!;

        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            TickerSymbol = product.TickerSymbol,
            StockCount = product.StockCount,
            CurrentPrice = product.CurrentPrice,
            OriginPrice = product.OriginPrice,
            PriceAlertThreshold = product.PriceAlertThreshold
        };
    }

    public static IEnumerable<ProductResponse> ToResponse(this IEnumerable<Product> products)
    {
        return products.Select(ToResponse);
    }

    public static Product ToEntity(this ProductRequest request)
    {
        if (request is null) return null!;

        return new Product
        {
            Name = request.Name ?? string.Empty,
            TickerSymbol = request.TickerSymbol ?? string.Empty,
            StockCount = request.StockCount,
            OriginPrice = request.Price,
            CurrentPrice = 0, // Always start at 0 until synced
            PriceAlertThreshold = request.PriceAlertThreshold ?? 0.1, // Default 10%
            StockAlertThreshold = request.StockAlertThreshold ?? 0    // Default 0 (no alert)
        };
    }

    public static IEnumerable<Product> ToEntity(this IEnumerable<ProductRequest> requests)
    {
        return requests.Select(ToEntity);
    }
}
