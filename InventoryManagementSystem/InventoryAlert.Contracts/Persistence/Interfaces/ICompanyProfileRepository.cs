using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface ICompanyProfileRepository : IGenericRepository<CompanyProfile>
{
    Task<CompanyProfile?> GetBySymbolAsync(string symbol, CancellationToken ct = default);
}
