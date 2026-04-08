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
    public DbSet<Watchlist> Watchlists { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }
    public DbSet<CompanyProfile> CompanyProfiles { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
