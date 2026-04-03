using InventoryAlert.Api.Domain.Entities;
using InventoryAlert.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence.Repositories
{
    public class ProductRepository(AppDbContext dbContext)
        : GenericRepository<Product>(dbContext), IProductRepository
    {
        public DbSet<Product> Products { get; } = dbContext.Products;
    }
}
