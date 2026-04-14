using InventoryAlert.Domain.Entities.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Infrastructure.Persistence.Postgres.Configurations;

public class StockMetricConfiguration : IEntityTypeConfiguration<StockMetric>
{
    public void Configure(EntityTypeBuilder<StockMetric> builder)
    {
        builder.ToTable("stock_metrics");

        builder.HasKey(x => x.TickerSymbol);

        builder.Property(x => x.TickerSymbol)
            .HasMaxLength(10);

        builder.HasOne<StockListing>()
            .WithOne()
            .HasPrincipalKey<StockListing>(x => x.TickerSymbol)
            .HasForeignKey<StockMetric>(x => x.TickerSymbol)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
