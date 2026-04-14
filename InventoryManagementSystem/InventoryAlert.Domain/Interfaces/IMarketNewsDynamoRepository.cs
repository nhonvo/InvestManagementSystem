using InventoryAlert.Domain.Entities.Dynamodb;

namespace InventoryAlert.Domain.Interfaces;

public interface IMarketNewsDynamoRepository : IDynamoDbGenericRepository<MarketNewsDynamoEntry>
{
}


