using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAlert.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Severity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "notifications");
        }
    }
}
