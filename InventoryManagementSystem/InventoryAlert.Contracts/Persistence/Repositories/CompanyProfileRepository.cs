using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class CompanyProfileRepository(InventoryDbContext context)
    : GenericRepository<CompanyProfile>(context), ICompanyProfileRepository
{
    private readonly InventoryDbContext _context = context;

    public async Task<CompanyProfile?> GetBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        return await _context.CompanyProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Symbol == symbol, ct);
    }
}
