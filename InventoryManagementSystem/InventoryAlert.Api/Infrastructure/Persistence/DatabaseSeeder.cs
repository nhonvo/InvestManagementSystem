using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed data initializer for Development and Docker environments.
/// Runs automatically on startup if the Products table is empty.
/// </summary>
public static class DatabaseSeeder
{

    public static async Task SeedAsync(InventoryDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("[Seeder] Users table already populated. Skipping seed.");
            return;
        }

        logger.LogInformation("[Seeder] Seeding Users...");

        await db.Users.AddRangeAsync(
        [
                new() {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new ()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Username = "user1",
                    Email = "user1@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
            ]
        );
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Seeder] Seeded users successfully.");
    }
}
