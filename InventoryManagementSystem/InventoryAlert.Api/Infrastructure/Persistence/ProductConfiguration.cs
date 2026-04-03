using InventoryAlert.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Api.Infrastructure.Persistence
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(p => p.Name).IsUnique();
            builder.HasIndex(p => p.TickerSymbol).IsUnique(); // Index for performance

            // Stock Mapping
            builder.Property(p => p.StockCount)
                .IsRequired()
                .HasDefaultValue(0);

            // Pricing Mapping
            builder.Property(p => p.OriginPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.CurrentPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.PriceAlertThreshold)
                .IsRequired()
                .HasDefaultValue(0.2); // Default 20% surge

            builder.Property(p => p.TickerSymbol)
                .HasMaxLength(15);

            builder.Property(p => p.LastAlertSentAt)
                .IsRequired(false);
        }
    }
}
