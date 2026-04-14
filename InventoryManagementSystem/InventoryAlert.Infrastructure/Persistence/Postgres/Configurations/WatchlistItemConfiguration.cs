using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Configurations;

public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.ToTable("watchlist_items");

        // Composite PK
        builder.HasKey(x => new { x.UserId, x.TickerSymbol });

        builder.Property(x => x.TickerSymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<StockListing>()
            .WithMany()
            .HasPrincipalKey(x => x.TickerSymbol)
            .HasForeignKey(x => x.TickerSymbol)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
