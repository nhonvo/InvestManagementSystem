using InventoryAlert.Contracts.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAlert.Api.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class updateSeedData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 1,
            column: "CurrentPrice",
            value: 300m);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "Products",
            keyColumn: "Id",
            keyValue: 1,
            column: "CurrentPrice",
            value: 250m);
    }
}

