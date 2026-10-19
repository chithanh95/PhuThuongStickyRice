using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019134600_AddPerfHuntIndexes10")]
    public partial class AddPerfHuntIndexes10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 13:35):
            // DiscountCodes: evaluate-best mỗi checkout scan full table do chỉ có index Code unique.
            // Thêm index IsActive + CreatedDateTime để serve WHERE IsActive + (OwnerPhone null/empty, !expired, uses-left) ORDER BY CreatedDateTime DESC.
            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_IsActive_CreatedDateTime",
                table: "DiscountCodes",
                columns: new[] { "IsActive", "CreatedDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DiscountCodes_IsActive_CreatedDateTime",
                table: "DiscountCodes");
        }
    }
}
