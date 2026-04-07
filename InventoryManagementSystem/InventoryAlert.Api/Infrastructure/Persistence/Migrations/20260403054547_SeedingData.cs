using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SeedingData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Products",
            columns: new[] { "Id", "AlertThreshold", "LastAlertSentAt", "Name", "StockCount", "TickerSymbol" },
            values: new object[,]
            {
                { 1, 5, null, "Apple", 10, "AAPL" },
                { 2, 8, null, "Google", 3, "GOOGL" },
                { 3, 10, null, "Microsoft", 20, "MSFT" }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 1);

        migrationBuilder.DeleteData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 2);

        migrationBuilder.DeleteData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 3);
    }
}

