using InventoryAlert.Contracts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAlert.Contracts.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.Username).IsRequired().HasMaxLength(64);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(128);

        builder.HasData(
            new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = "$2a$11$qlQhPRy3I8X3WXtMQ9AWcO4pGzSMlboi.8p.PyQvar8aakes5yVFi",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "user1",
                Email = "user1@example.com",
                PasswordHash = "$2a$11$qlQhPRy3I8X3WXtMQ9AWcO4pGzSMlboi.8p.PyQvar8aakes5yVFi",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
