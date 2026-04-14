using InventoryAlert.Domain.Entities.Postgres;

namespace InventoryAlert.Domain.Interfaces;

public interface IStockListingRepository : IGenericRepository<StockListing>
{
    Task<StockListing?> FindBySymbolAsync(string symbol, CancellationToken ct);
    Task<IEnumerable<StockListing>> SearchAsync(string query, CancellationToken ct);
}
