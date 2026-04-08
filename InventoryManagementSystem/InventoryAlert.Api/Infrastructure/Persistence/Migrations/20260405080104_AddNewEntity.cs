using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddNewEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "StockAlertThreshold",
            table: "Products",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "EarningsRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Period = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                ActualEPS = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                EstimatedEPS = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                SurprisePercent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EarningsRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_EarningsRecords_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EventLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Payload = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                SourceService = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventLogs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "InsiderTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                InsiderName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                TransactionType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                Shares = table.Column<long>(type: "bigint", nullable: false),
                ValueUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InsiderTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_InsiderTransactions_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NewsRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProductId = table.Column<int>(type: "integer", nullable: false),
                Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                Headline = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NewsRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_NewsRecords_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EarningsRecords_ProductId",
            table: "EarningsRecords",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_EventLogs_MessageId",
            table: "EventLogs",
            column: "MessageId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InsiderTransactions_ProductId",
            table: "InsiderTransactions",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_NewsRecords_ProductId",
            table: "NewsRecords",
            column: "ProductId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EarningsRecords");

        migrationBuilder.DropTable(
            name: "EventLogs");

        migrationBuilder.DropTable(
            name: "InsiderTransactions");

        migrationBuilder.DropTable(
            name: "NewsRecords");

        migrationBuilder.DropColumn(
            name: "StockAlertThreshold",
            table: "Products");
    }
}

