using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryAlert.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToFinanceV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: true),
                    Low = table.Column<decimal>(type: "numeric", nullable: true),
                    Open = table.Column<decimal>(type: "numeric", nullable: true),
                    PrevClose = table.Column<decimal>(type: "numeric", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stock_listings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Exchange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Country = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MarketCap = table.Column<decimal>(type: "numeric", nullable: true),
                    Ipo = table.Column<DateOnly>(type: "date", nullable: true),
                    WebUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Logo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastProfileSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_listings", x => x.Id);
                    table.UniqueConstraint("AK_stock_listings_TickerSymbol", x => x.TickerSymbol);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "earnings_surprises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Period = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualEps = table.Column<double>(type: "double precision", nullable: true),
                    EstimateEps = table.Column<double>(type: "double precision", nullable: true),
                    SurprisePercent = table.Column<double>(type: "double precision", nullable: true),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_earnings_surprises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_earnings_surprises_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "insider_transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Share = table.Column<long>(type: "bigint", nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FilingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TransactionCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insider_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_insider_transactions_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_trends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Period = table.Column<string>(type: "text", nullable: false),
                    StrongBuy = table.Column<int>(type: "integer", nullable: false),
                    Buy = table.Column<int>(type: "integer", nullable: false),
                    Hold = table.Column<int>(type: "integer", nullable: false),
                    Sell = table.Column<int>(type: "integer", nullable: false),
                    StrongSell = table.Column<int>(type: "integer", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recommendation_trends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recommendation_trends_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_metrics",
                columns: table => new
                {
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PeRatio = table.Column<double>(type: "double precision", nullable: true),
                    PbRatio = table.Column<double>(type: "double precision", nullable: true),
                    EpsBasicTtm = table.Column<double>(type: "double precision", nullable: true),
                    DividendYield = table.Column<double>(type: "double precision", nullable: true),
                    Week52High = table.Column<decimal>(type: "numeric", nullable: true),
                    Week52Low = table.Column<decimal>(type: "numeric", nullable: true),
                    RevenueGrowthTtm = table.Column<double>(type: "double precision", nullable: true),
                    MarginNet = table.Column<double>(type: "double precision", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_metrics", x => x.TickerSymbol);
                    table.ForeignKey(
                        name: "FK_stock_metrics_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    TargetValue = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TriggerOnce = table.Column<bool>(type: "boolean", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alert_rules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_alert_rules_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_trades_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_trades_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "watchlist_items",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist_items", x => new { x.UserId, x.TickerSymbol });
                    table.ForeignKey(
                        name: "FK_watchlist_items_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_watchlist_items_stock_listings_TickerSymbol",
                        column: x => x.TickerSymbol,
                        principalTable: "stock_listings",
                        principalColumn: "TickerSymbol",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    TickerSymbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notifications_alert_rules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "alert_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_rules_TickerSymbol_IsActive",
                table: "alert_rules",
                columns: new[] { "TickerSymbol", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_alert_rules_UserId",
                table: "alert_rules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_earnings_surprises_TickerSymbol_Period",
                table: "earnings_surprises",
                columns: new[] { "TickerSymbol", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_insider_transactions_TickerSymbol_TransactionDate",
                table: "insider_transactions",
                columns: new[] { "TickerSymbol", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_AlertRuleId",
                table: "notifications",
                column: "AlertRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead_CreatedAt",
                table: "notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_price_history_TickerSymbol_RecordedAt",
                table: "price_history",
                columns: new[] { "TickerSymbol", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_recommendation_trends_TickerSymbol_Period",
                table: "recommendation_trends",
                columns: new[] { "TickerSymbol", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_listings_TickerSymbol",
                table: "stock_listings",
                column: "TickerSymbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trades_TickerSymbol",
                table: "trades",
                column: "TickerSymbol");

            migrationBuilder.CreateIndex(
                name: "IX_trades_UserId_TickerSymbol_TradedAt",
                table: "trades",
                columns: new[] { "UserId", "TickerSymbol", "TradedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_watchlist_items_TickerSymbol",
                table: "watchlist_items",
                column: "TickerSymbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "earnings_surprises");

            migrationBuilder.DropTable(
                name: "insider_transactions");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "price_history");

            migrationBuilder.DropTable(
                name: "recommendation_trends");

            migrationBuilder.DropTable(
                name: "stock_metrics");

            migrationBuilder.DropTable(
                name: "trades");

            migrationBuilder.DropTable(
                name: "watchlist_items");

            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "stock_listings");
        }
    }
}
