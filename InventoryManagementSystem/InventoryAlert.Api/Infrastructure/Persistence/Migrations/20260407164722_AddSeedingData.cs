using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AlertRules",
                columns: new[] { "Id", "CreatedAt", "Field", "IsActive", "LastTriggeredAt", "NotifyChannel", "Operator", "Symbol", "Threshold", "UserId" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "price", true, null, "telegram", "gt", "AAPL", 250m, "00000000-0000-0000-0000-000000000001" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "price", true, null, "telegram", "lt", "MSFT", 350m, "11111111-1111-1111-1111-111111111111" }
                });

            migrationBuilder.InsertData(
                table: "CompanyProfiles",
                columns: new[] { "Symbol", "Country", "Currency", "Exchange", "Industry", "IpoDate", "Logo", "MarketCap", "Name", "RefreshedAt", "WebUrl" },
                values: new object[,]
                {
                    { "AAPL", null, "USD", "NASDAQ", "Technology", null, "https://static2.finnhub.io/logo/8743234a-800d-11ea-8020-000000000001.png", null, "Apple Inc", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "GOOGL", null, "USD", "NASDAQ", "Technology", null, "https://static2.finnhub.io/logo/8d68923a-800d-11ea-9c09-000000000004.png", null, "Alphabet Inc", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { "MSFT", null, "USD", "NASDAQ", "Technology", null, "https://static2.finnhub.io/logo/829651a0-800d-11ea-8951-000000000003.png", null, "Microsoft Corp", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "PasswordHash", "Username" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@example.com", "$2a$11$qlQhPRy3I8X3WXtMQ9AWcO4pGzSMlboi.8p.PyQvar8aakes5yVFi", "admin" },
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "user1@example.com", "$2a$11$qlQhPRy3I8X3WXtMQ9AWcO4pGzSMlboi.8p.PyQvar8aakes5yVFi", "user1" }
                });

            migrationBuilder.InsertData(
                table: "Watchlists",
                columns: new[] { "Id", "AddedAt", "Symbol", "UserId" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AAPL", "00000000-0000-0000-0000-000000000001" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MSFT", "00000000-0000-0000-0000-000000000001" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "GOOGL", "11111111-1111-1111-1111-111111111111" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "CompanyProfiles",
                keyColumn: "Symbol",
                keyValue: "AAPL");

            migrationBuilder.DeleteData(
                table: "CompanyProfiles",
                keyColumn: "Symbol",
                keyValue: "GOOGL");

            migrationBuilder.DeleteData(
                table: "CompanyProfiles",
                keyColumn: "Symbol",
                keyValue: "MSFT");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Watchlists",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "Watchlists",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "Watchlists",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        }
    }
}
