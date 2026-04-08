using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class renameFieldProductEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AlertThreshold",
            table: "Products");

        migrationBuilder.RenameColumn(
            name: "Price",
            table: "Products",
            newName: "OriginPrice");

        migrationBuilder.AlterColumn<string>(
            name: "TickerSymbol",
            table: "Products",
            type: "character varying(15)",
            maxLength: 15,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "character varying(10)",
            oldMaxLength: 10,
            oldNullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "CurrentPrice",
            table: "Products",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<double>(
            name: "PriceAlertThreshold",
            table: "Products",
            type: "double precision",
            nullable: false,
            defaultValue: 0.20000000000000001);

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 1,
            columns: new[] { "CurrentPrice", "PriceAlertThreshold", "StockCount" },
            values: new object[] { 250m, 0.20000000000000001, 50 });

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 2,
            columns: new[] { "CurrentPrice", "PriceAlertThreshold", "StockCount" },
            values: new object[] { 300m, 0.10000000000000001, 100 });

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 3,
            columns: new[] { "CurrentPrice", "PriceAlertThreshold", "StockCount" },
            values: new object[] { 400m, 0.14999999999999999, 5 });

        migrationBuilder.CreateIndex(
            name: "IX_Products_TickerSymbol",
            table: "Products",
            column: "TickerSymbol",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Products_TickerSymbol",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "CurrentPrice",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "PriceAlertThreshold",
            table: "Products");

        migrationBuilder.RenameColumn(
            name: "OriginPrice",
            table: "Products",
            newName: "Price");

        migrationBuilder.AlterColumn<string>(
            name: "TickerSymbol",
            table: "Products",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(15)",
            oldMaxLength: 15);

        migrationBuilder.AddColumn<double>(
            name: "AlertThreshold",
            table: "Products",
            type: "double precision",
            nullable: false,
            defaultValue: 5.0);

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 1,
            columns: new[] { "AlertThreshold", "StockCount" },
            values: new object[] { 0.20000000000000001, 10 });

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 2,
            columns: new[] { "AlertThreshold", "StockCount" },
            values: new object[] { 0.10000000000000001, 3 });

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 3,
            columns: new[] { "AlertThreshold", "StockCount" },
            values: new object[] { 0.10000000000000001, 20 });
    }
}

