using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;

public class AlertRuleRepository(AppDbContext context)
    : GenericRepository<AlertRule>(context), IAlertRuleRepository
{
    public async Task<IEnumerable<AlertRule>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var userGuid = Guid.Parse(userId);
        return await _dbSet.AsNoTracking()
            .Where(x => x.UserId == userGuid)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<AlertRule>> GetBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(x => x.TickerSymbol == symbol && x.IsActive)
            .ToListAsync(ct);
    }
}
