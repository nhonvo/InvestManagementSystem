using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Contracts.Persistence.Configurations;

public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.ToTable("CompanyProfiles");
        builder.HasKey(c => c.Symbol);

        builder.HasData(
            new CompanyProfile
            {
                Symbol = "AAPL",
                Name = "Apple Inc",
                Exchange = "NASDAQ",
                Currency = "USD",
                Industry = "Technology",
                Logo = "https://static2.finnhub.io/logo/8743234a-800d-11ea-8020-000000000001.png",
                RefreshedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CompanyProfile
            {
                Symbol = "MSFT",
                Name = "Microsoft Corp",
                Exchange = "NASDAQ",
                Currency = "USD",
                Industry = "Technology",
                Logo = "https://static2.finnhub.io/logo/829651a0-800d-11ea-8951-000000000003.png",
                RefreshedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new CompanyProfile
            {
                Symbol = "GOOGL",
                Name = "Alphabet Inc",
                Exchange = "NASDAQ",
                Currency = "USD",
                Industry = "Technology",
                Logo = "https://static2.finnhub.io/logo/8d68923a-800d-11ea-9c09-000000000004.png",
                RefreshedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
