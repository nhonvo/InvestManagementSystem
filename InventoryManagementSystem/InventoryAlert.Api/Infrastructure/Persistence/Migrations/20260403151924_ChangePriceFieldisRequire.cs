using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ChangePriceFieldisRequire : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Price",
            table: "Products",
            type: "numeric(18,2)",
            nullable: false,
            defaultValue: 0m,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Price",
            table: "Products",
            type: "numeric(18,2)",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)");
    }
}

