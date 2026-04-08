using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddPriceField : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Price",
            table: "Products",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 1,
            column: "Price",
            value: 250m);

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 2,
            column: "Price",
            value: 300m);

        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 3,
            column: "Price",
            value: 400m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Price",
            table: "Products");
    }
}

