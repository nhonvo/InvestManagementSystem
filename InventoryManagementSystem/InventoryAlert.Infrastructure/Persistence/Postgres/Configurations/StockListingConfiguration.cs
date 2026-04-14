using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Configurations;

public class StockListingConfiguration : IEntityTypeConfiguration<StockListing>
{
    public void Configure(EntityTypeBuilder<StockListing> builder)
    {
        builder.ToTable("stock_listings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TickerSymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(x => x.TickerSymbol)
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Exchange).HasMaxLength(50);
        builder.Property(x => x.Currency).HasMaxLength(10);
        builder.Property(x => x.Country).HasMaxLength(10);
        builder.Property(x => x.Industry).HasMaxLength(100);
        builder.Property(x => x.Logo).HasMaxLength(1000);
        builder.Property(x => x.WebUrl).HasMaxLength(1000);
    }
}
