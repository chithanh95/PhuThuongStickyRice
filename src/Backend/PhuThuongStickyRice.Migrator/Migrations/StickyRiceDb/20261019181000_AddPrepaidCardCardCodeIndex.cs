using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019181000_AddPrepaidCardCardCodeIndex")]
    public partial class AddPrepaidCardCardCodeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 18:05): unique index trên
            // PrepaidCards(CardCode) — lookup CardCode là thao tác real-time
            // tại quầy (TryRedeemAsync ExecuteUpdateAsync + RedeemAsync
            // 2 lần FirstOrDefaultAsync + check trùng khi tạo thẻ), trước
            // đây toàn là clustered index scan vì bảng không có index nào.
            // Uniqueness khớp ngữ nghĩa nghiệp vụ (mã TX+6 số in hoa duy
            // nhất). Non-clustered unique, lookup-only.
            migrationBuilder.CreateIndex(
                name: "IX_PrepaidCards_CardCode",
                table: "PrepaidCards",
                column: "CardCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrepaidCards_CardCode",
                table: "PrepaidCards");
        }
    }
}
