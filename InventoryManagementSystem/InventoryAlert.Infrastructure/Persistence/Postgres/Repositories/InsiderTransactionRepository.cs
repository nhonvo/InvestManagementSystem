using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class InsiderTransactionRepository(AppDbContext context) : IInsiderTransactionRepository
{
    private readonly DbSet<InsiderTransaction> _dbSet = context.InsiderTransactions;

    public async Task<IEnumerable<InsiderTransaction>> GetBySymbolAsync(string symbol, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
            .Where(x => x.TickerSymbol == symbol)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(ct);
    }

    public async Task ReplaceForSymbolAsync(string symbol, IEnumerable<InsiderTransaction> entries, CancellationToken ct)
    {
        // Simple replace strategy for insider filings as they are historical assets but we only keep the latest 100 per spec
        await _dbSet.Where(x => x.TickerSymbol == symbol).ExecuteDeleteAsync(ct);
        await _dbSet.AddRangeAsync(entries, ct);
    }
}
