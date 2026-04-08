using InventoryAlert.Contracts.Persistence.Interfaces;
using InventoryAlert.Contracts.Persistence;
using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class ProductRepository(InventoryDbContext context) : GenericRepository<Product>(context), IProductRepository
{
    private readonly DbSet<Product> _products = context.Products;

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        string? name, int? minStock, int? maxStock, string? sortBy,
        int pageNumber, int pageSize, CancellationToken ct)
    {
        IQueryable<Product> query = _products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => p.Name.Contains(name));

        if (minStock.HasValue) query = query.Where(p => p.StockCount >= minStock.Value);
        if (maxStock.HasValue) query = query.Where(p => p.StockCount <= maxStock.Value);

        query = sortBy?.ToLowerInvariant() switch
        {
            "name_desc" => query.OrderByDescending(p => p.Name),
            "price_asc" => query.OrderBy(p => p.CurrentPrice),
            "price_desc" => query.OrderByDescending(p => p.CurrentPrice),
            "stock_asc" => query.OrderBy(p => p.StockCount),
            "stock_desc" => query.OrderByDescending(p => p.StockCount),
            _ => query.OrderBy(p => p.Name)   // default
        };

        // Sanitization
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, Math.Min(50, pageSize));

        var skip = (pageNumber - 1) * pageSize;
        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public async Task<Product?> GetByTickerAsync(string ticker, CancellationToken ct)
    {
        return await _products.AsNoTracking().FirstOrDefaultAsync(p => p.TickerSymbol == ticker, ct);
    }

    public async Task<IEnumerable<string>> GetExistingTickersAsync(IEnumerable<string> tickers, CancellationToken ct)
    {
        return await _products
            .AsNoTracking()
            .Where(p => tickers.Contains(p.TickerSymbol))
            .Select(p => p.TickerSymbol)
            .ToListAsync(ct);
    }
}
