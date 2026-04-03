using InventoryAlert.Api.Domain.Entities;
using InventoryAlert.Api.Infrastructure.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories
{
    public class ProductRepository(AppDbContext dbContext) : GenericRepository<Product>(dbContext), IProductRepository
    {
        public DbSet<Product> _dbSet = dbContext.Products;
    }
}
