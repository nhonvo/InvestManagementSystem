namespace InventoryAlert.Api.Domain.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? name, int? minStock, int? maxStock, string? sortBy,
        int pageNumber, int pageSize, CancellationToken ct);
    Task<Product?> GetByTickerAsync(string ticker, CancellationToken ct);
    Task<IEnumerable<string>> GetExistingTickersAsync(IEnumerable<string> tickers, CancellationToken ct);
}
