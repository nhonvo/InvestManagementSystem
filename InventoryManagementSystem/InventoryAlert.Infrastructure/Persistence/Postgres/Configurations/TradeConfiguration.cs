using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Configurations;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trades");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TickerSymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(x => new { x.UserId, x.TickerSymbol, x.TradedAt });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<StockListing>()
            .WithMany()
            .HasPrincipalKey(x => x.TickerSymbol)
            .HasForeignKey(x => x.TickerSymbol)
            .OnDelete(DeleteBehavior.Restrict); // Keep record of trade even if symbol is removed (though unlikely)
    }
}
