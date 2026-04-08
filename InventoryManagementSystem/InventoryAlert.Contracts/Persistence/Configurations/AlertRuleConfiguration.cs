using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Contracts.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("AlertRules");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.Symbol).HasDatabaseName("idx_alertrules_symbol");
        builder.HasIndex(a => a.UserId).HasDatabaseName("idx_alertrules_user");

        builder.HasData(
            new AlertRule
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = "00000000-0000-0000-0000-000000000001",
                Symbol = "AAPL",
                Field = "price",
                Operator = "gt",
                Threshold = 250m,
                NotifyChannel = "telegram",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AlertRule
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = "11111111-1111-1111-1111-111111111111",
                Symbol = "MSFT",
                Field = "price",
                Operator = "lt",
                Threshold = 350m,
                NotifyChannel = "telegram",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
