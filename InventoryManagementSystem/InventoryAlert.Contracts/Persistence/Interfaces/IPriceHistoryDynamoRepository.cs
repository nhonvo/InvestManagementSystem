using InventoryAlert.Contracts.Persistence.Entities;

namespace InventoryAlert.Contracts.Persistence.Interfaces;

public interface IPriceHistoryDynamoRepository : IDynamoDbGenericRepository<PriceHistoryEntry>
{
}
