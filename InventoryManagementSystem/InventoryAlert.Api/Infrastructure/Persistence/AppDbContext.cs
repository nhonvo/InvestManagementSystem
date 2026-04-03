using InventoryAlert.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace InventoryAlert.Api.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Apple",
                    TickerSymbol = "AAPL",
                    StockCount = 50,
                    OriginPrice = 250,
                    CurrentPrice = 200,
                    PriceAlertThreshold = 0.2
                },
                new Product
                {
                    Id = 2,
                    Name = "Google",
                    TickerSymbol = "GOOGL",
                    StockCount = 100,
                    OriginPrice = 300,
                    CurrentPrice = 300,
                    PriceAlertThreshold = 0.1
                },
                new Product
                {
                    Id = 3,
                    Name = "Microsoft",
                    TickerSymbol = "MSFT",
                    StockCount = 5,
                    OriginPrice = 400,
                    CurrentPrice = 400,
                    PriceAlertThreshold = 0.15
                }
            );
        }
    }
}
