using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019141500_AddPerfHuntIndexes12")]
    public partial class AddPerfHuntIndexes12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 14:05):
            // WalletAppService.GetTransactionsAsync (admin tra cứu ví theo SĐT):
            // WHERE Phone == x ORDER BY CreatedAt DESC, gọi mỗi lần tra cứu ví;
            // bảng WalletTransactions tăng vô hạn (không bao giờ xóa). Index đơn
            // IX_WalletTransactions_Phone chỉ seek được Phone, sort phải làm trên
            // memory theo từng SĐT. Composite (Phone, CreatedAt): seek + sort ngay
            // trên index. Non-unique, lookup-only, không đổi nghiệp vụ.
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_Phone",
                table: "WalletTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Phone_CreatedAt",
                table: "WalletTransactions",
                columns: new[] { "Phone", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_Phone_CreatedAt",
                table: "WalletTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Phone",
                table: "WalletTransactions",
                column: "Phone");
        }
    }
}
