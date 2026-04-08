using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryAlert.Api.Infrastructure.Persistence;

/// <summary>
/// Idempotent seed data initializer for Development and Docker environments.
/// Runs automatically on startup if the Products table is empty.
/// </summary>
public static class DatabaseSeeder
{
    private static readonly (string Symbol, string Name, string Exchange, string Type)[] SeedSymbols =
    [
        ("AAPL",  "Apple Inc.",                      "NASDAQ", "stock"),
        ("MSFT",  "Microsoft Corporation",            "NASDAQ", "stock"),
        ("GOOGL", "Alphabet Inc.",                    "NASDAQ", "stock"),
        ("AMZN",  "Amazon.com Inc.",                  "NASDAQ", "stock"),
        ("NVDA",  "NVIDIA Corporation",               "NASDAQ", "stock"),
        ("META",  "Meta Platforms Inc.",              "NASDAQ", "stock"),
        ("TSLA",  "Tesla Inc.",                       "NASDAQ", "stock"),
        ("NFLX",  "Netflix Inc.",                     "NASDAQ", "stock"),
        ("AMD",   "Advanced Micro Devices Inc.",      "NASDAQ", "stock"),
        ("INTC",  "Intel Corporation",                "NASDAQ", "stock"),
        ("JPM",   "JPMorgan Chase & Co.",             "NYSE",   "stock"),
        ("BAC",   "Bank of America Corporation",      "NYSE",   "stock"),
        ("GS",    "Goldman Sachs Group Inc.",         "NYSE",   "stock"),
        ("V",     "Visa Inc.",                        "NYSE",   "stock"),
        ("MA",    "Mastercard Incorporated",          "NYSE",   "stock"),
        ("JNJ",   "Johnson & Johnson",                "NYSE",   "stock"),
        ("PFE",   "Pfizer Inc.",                      "NYSE",   "stock"),
        ("UNH",   "UnitedHealth Group Incorporated",  "NYSE",   "stock"),
        ("XOM",   "Exxon Mobil Corporation",          "NYSE",   "stock"),
        ("CVX",   "Chevron Corporation",              "NYSE",   "stock"),
    ];

    public static async Task SeedAsync(InventoryDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct))
        {
            logger.LogInformation("[Seeder] Products table already populated. Skipping seed.");
            return;
        }

        logger.LogInformation("[Seeder] Seeding {Count} products...", SeedSymbols.Length);

        var products = SeedSymbols.Select((s, i) => new Product
        {
            Name = s.Name,
            TickerSymbol = s.Symbol,
            OriginPrice = 100m + (i * 17.5m),
            CurrentPrice = 100m + (i * 17.5m),
            PriceAlertThreshold = 0.05,
            StockCount = 1000,
            StockAlertThreshold = 50
        });

        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Seeder] Seeded {Count} products successfully.", SeedSymbols.Length);
    }
}
