using System.Reflection;
using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Contracts.Persistence;

/// <summary>
/// Unified DbContext for the entire system, shared across API, Worker, and testing.
/// </summary>
public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Product>().ToTable("Products");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
