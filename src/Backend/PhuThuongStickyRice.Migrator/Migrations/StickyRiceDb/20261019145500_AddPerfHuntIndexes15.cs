using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019145500_AddPerfHuntIndexes15")]
    public partial class AddPerfHuntIndexes15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 14:46): batch index còn thiếu từ scan
            // trực tiếp. Tất cả non-unique, lookup-only, không đổi nghiệp vụ.

            // (1) Articles: ArticleAppService.GetListAsync chạy
            // OrderByDescending(PublishedAt) + Take(200) mỗi lần admin mở
            // trang bài viết (bảng tăng theo thời gian); GetPublishedAsync
            // (đã cache 5 phút) cũng sort theo PublishedAt.
            migrationBuilder.CreateIndex(
                name: "IX_Articles_PublishedAt",
                table: "Articles",
                column: "PublishedAt");

            // (2) CungReminders: QuoteRequestAppService.GetRemindersAsync chạy
            // OrderByDescending(CreatedAt) + Take(200) mỗi lần admin mở trang
            // nhắc cúng (bảng tăng vô hạn, không retention) — sort toàn bảng.
            migrationBuilder.CreateIndex(
                name: "IX_CungReminders_CreatedAt",
                table: "CungReminders",
                column: "CreatedAt");

            // (3) QuoteRequests: QuoteRequestAppService.GetListAsync lọc
            // Where(Status) khi admin chọn trạng thái — không index thì
            // full scan toàn bảng (đã có IX_CreatedAt cho sort).
            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_Status",
                table: "QuoteRequests",
                column: "Status");

            // (4) LoyaltyAccounts: LoyaltyAppService.GetListAsync chạy
            // OrderByDescending(TotalSpent) + Take(200) mỗi lần admin mở
            // trang tài khoản điểm (bảng tăng theo mỗi đơn hàng).
            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyAccounts_TotalSpent",
                table: "LoyaltyAccounts",
                column: "TotalSpent");

            // (5) WalletAccounts: WalletAppService.GetListAsync chạy
            // OrderByDescending(Balance) + Take(200) mỗi lần admin mở
            // trang ví tiền (bảng tăng theo mỗi khách).
            migrationBuilder.CreateIndex(
                name: "IX_WalletAccounts_Balance",
                table: "WalletAccounts",
                column: "Balance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletAccounts_Balance",
                table: "WalletAccounts");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyAccounts_TotalSpent",
                table: "LoyaltyAccounts");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_Status",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_CungReminders_CreatedAt",
                table: "CungReminders");

            migrationBuilder.DropIndex(
                name: "IX_Articles_PublishedAt",
                table: "Articles");
        }
    }
}
