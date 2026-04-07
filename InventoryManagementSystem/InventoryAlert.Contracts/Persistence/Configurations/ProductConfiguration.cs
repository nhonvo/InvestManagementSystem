using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Contracts.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.TickerSymbol).IsRequired().HasMaxLength(16);
        builder.Property(x => x.OriginPrice).HasPrecision(18, 4);
        builder.Property(x => x.CurrentPrice).HasPrecision(18, 4);

        builder.HasData(
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
