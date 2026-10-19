using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019090000_AddPrepaidCardWeeklyPlan")]
    public partial class AddPrepaidCardWeeklyPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Item 164: thẻ xôi sáng trả trước.
            migrationBuilder.CreateTable(
                name: "PrepaidCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TotalSuits = table.Column<int>(type: "int", nullable: false),
                    UsedSuits = table.Column<int>(type: "int", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrepaidCards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrepaidCards_CardCode",
                table: "PrepaidCards",
                column: "CardCode",
                unique: true,
                filter: "[CardCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PrepaidCards_CustomerPhone",
                table: "PrepaidCards",
                column: "CustomerPhone");

            // Item 185: gói ăn sáng cả tuần.
            migrationBuilder.CreateTable(
                name: "WeeklyPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayMenusJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyPlanOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeeklyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimedDaysJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyPlanOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlanOrders_WeeklyPlanId",
                table: "WeeklyPlanOrders",
                column: "WeeklyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlanOrders_CustomerPhone",
                table: "WeeklyPlanOrders",
                column: "CustomerPhone");

            // Item 161: cọc khi duyệt báo giá.
            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "QuoteRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositInfo",
                table: "QuoteRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DepositAmount", table: "QuoteRequests");
            migrationBuilder.DropColumn(name: "DepositInfo", table: "QuoteRequests");
            migrationBuilder.DropTable(name: "WeeklyPlanOrders");
            migrationBuilder.DropTable(name: "WeeklyPlans");
            migrationBuilder.DropTable(name: "PrepaidCards");
        }
    }
}
