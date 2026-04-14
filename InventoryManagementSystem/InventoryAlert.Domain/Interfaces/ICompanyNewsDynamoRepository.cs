using InventoryAlert.Domain.Entities.Dynamodb;

namespace InventoryAlert.Domain.Interfaces;

public interface ICompanyNewsDynamoRepository : IDynamoDbGenericRepository<CompanyNewsDynamoEntry>
{
    Task<IEnumerable<CompanyNewsDynamoEntry>> GetLatestBySymbolAsync(string symbol, int limit, CancellationToken ct);
}
