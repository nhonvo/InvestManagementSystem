using InventoryAlert.Contracts.Persistence.Entities;

namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface INewsDynamoRepository : IDynamoDbGenericRepository<NewsDynamoEntry>
{
    Task<IEnumerable<NewsDynamoEntry>> GetNewsByTickerAsync(string tickerSymbol, int limit = 10, CancellationToken ct = default);
}
