using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019080000_AddPerfHuntIndexes")]
    public partial class AddPerfHuntIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: FunnelEvents grows one row per page view;
            // the admin funnel report filters on CreatedDateTime.
            migrationBuilder.CreateIndex(
                name: "IX_FunnelEvents_CreatedDateTime",
                table: "FunnelEvents",
                column: "CreatedDateTime");

            // perf-hunt 2026-10-19: phone-based order lookups on hot paths
            // (checkout pay-later/referral checks, customer order history).
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerPhone_Status",
                table: "Orders",
                columns: new[] { "CustomerPhone", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FunnelEvents_CreatedDateTime",
                table: "FunnelEvents");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerPhone_Status",
                table: "Orders");
        }
    }
}
