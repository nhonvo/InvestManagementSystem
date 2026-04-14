using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Configurations;

public class EarningsSurpriseConfiguration : IEntityTypeConfiguration<EarningsSurprise>
{
    public void Configure(EntityTypeBuilder<EarningsSurprise> builder)
    {
        builder.ToTable("earnings_surprises");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TickerSymbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(x => new { x.TickerSymbol, x.Period })
            .IsUnique();

        builder.HasOne<StockListing>()
            .WithMany()
            .HasPrincipalKey(x => x.TickerSymbol)
            .HasForeignKey(x => x.TickerSymbol)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
