using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Contracts.Persistence.Configurations;

public class WatchlistConfiguration : IEntityTypeConfiguration<Watchlist>
{
    public void Configure(EntityTypeBuilder<Watchlist> builder)
    {
        builder.ToTable("Watchlists");
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => w.UserId).HasDatabaseName("idx_watchlists_user");
        builder.HasIndex(w => new { w.UserId, w.Symbol }).IsUnique();
        builder.HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.Symbol)
            .HasPrincipalKey(p => p.TickerSymbol)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new Watchlist
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                UserId = "00000000-0000-0000-0000-000000000001",
                Symbol = "AAPL",
                AddedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Watchlist
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                UserId = "00000000-0000-0000-0000-000000000001",
                Symbol = "MSFT",
                AddedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Watchlist
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                UserId = "11111111-1111-1111-1111-111111111111",
                Symbol = "GOOGL",
                AddedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
