using InventoryAlert.Contracts.Entities;

namespace InventoryAlert.Api.Domain.Interfaces;

public interface ICompanyProfileRepository : IGenericRepository<CompanyProfile>
{
    Task<CompanyProfile?> GetBySymbolAsync(string symbol, CancellationToken ct = default);
}
