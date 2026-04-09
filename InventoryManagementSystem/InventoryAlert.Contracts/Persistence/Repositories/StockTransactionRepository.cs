using InventoryAlert.Contracts.Entities;
using InventoryAlert.Contracts.Persistence.Interfaces;

namespace InventoryAlert.Contracts.Persistence.Repositories;

public class StockTransactionRepository(InventoryDbContext context) 
    : GenericRepository<StockTransaction>(context), IStockTransactionRepository
{
}
