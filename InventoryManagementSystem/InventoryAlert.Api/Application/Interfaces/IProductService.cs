using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetProductsPagedAsync(ProductQueryParams queryParams, CancellationToken cancellationToken);
    Task<IEnumerable<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken);
    Task<ProductResponse?> GetProductByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductResponse> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateProductAsync(int id, ProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse?> DeleteProductAsync(int id, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateStockCountAsync(int id, int newCount, string userId, CancellationToken cancellationToken);
    Task BulkInsertProductsAsync(IEnumerable<ProductRequest> requests, CancellationToken cancellationToken);

    /// <summary>Returns products whose current price has dropped beyond their configured PriceAlertThreshold.
    /// </summary>
    Task<IEnumerable<PriceLossResponse>> GetPriceLossAlertsAsync(CancellationToken cancellationToken);

    /// <summary>Syncs the CurrentPrice of all products from Finnhub and persists in a single transaction.
    /// </summary>
    Task SyncCurrentPricesAsync(CancellationToken cancellationToken);
}
