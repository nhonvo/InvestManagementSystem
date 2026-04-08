using InventoryAlert.Api.Domain.Interfaces;
using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories;

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
