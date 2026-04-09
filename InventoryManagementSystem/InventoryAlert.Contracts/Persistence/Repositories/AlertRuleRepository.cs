using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class AlertRuleRepository(InventoryDbContext context)
    : GenericRepository<AlertRule>(context), IAlertRuleRepository
{
    private readonly InventoryDbContext _context = context;

    public async Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.AlertRules
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<IEnumerable<AlertRule>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _context.AlertRules
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }
}
