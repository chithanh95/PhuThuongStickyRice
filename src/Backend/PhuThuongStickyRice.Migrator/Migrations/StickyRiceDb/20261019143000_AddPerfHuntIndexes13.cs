using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019143000_AddPerfHuntIndexes13")]
    public partial class AddPerfHuntIndexes13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 14:15): batch index còn thiếu từ scan
            // run 14:05. Tất cả non-unique, lookup-only, không đổi nghiệp vụ.

            // (1) QuoteRequests: GetListAsync OrderByDescending(CreatedAt) +
            // LongCountAsync mỗi lần admin mở trang báo giá; bảng tăng theo
            // mỗi yêu cầu. Bảng này chưa có index nào.
            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_CreatedAt",
                table: "QuoteRequests",
                column: "CreatedAt");

            // (2) Subscriptions: GetListAsync OrderByDescending(CreatedAt) +
            // Take(200) mỗi lần admin mở trang; bảng này chưa có index nào.
            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions",
                column: "CreatedAt");

            // (3) CustomerPhotos: public gallery (cache-miss) và admin list
            // đều WHERE IsApproved + ORDER BY CreatedAt DESC; UGC tăng vô hạn.
            // Composite thay index đơn IsApproved: equality trên IsApproved
            // rồi sort ngay trên index.
            migrationBuilder.DropIndex(
                name: "IX_CustomerPhotos_IsApproved",
                table: "CustomerPhotos");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPhotos_IsApproved_CreatedAt",
                table: "CustomerPhotos",
                columns: new[] { "IsApproved", "CreatedAt" });

            // (4) DiscountCodes: admin list sort TRẦN theo CreatedDateTime
            // DESC + Take(500), không WHERE — composite
            // (IsActive, CreatedDateTime) không phục vụ ORDER BY trần cột
            // thứ 2. Bảng tăng vô hạn (spin/sinh nhật/reward-rule).
            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_CreatedDateTime",
                table: "DiscountCodes",
                column: "CreatedDateTime");

            // (5) PrepaidCards: GetListAsync OrderByDescending(CreatedAt) +
            // Take(clamp 200); thẻ không bao giờ xóa. Không có config class
            // cho entity này nên index chỉ khai báo trong migration thủ công
            // (theo tiền lệ IX_PrepaidCards_CardCode trong
            // 20261019090000_AddPrepaidCardWeeklyPlan).
            migrationBuilder.CreateIndex(
                name: "IX_PrepaidCards_CreatedAt",
                table: "PrepaidCards",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PrepaidCards_CreatedAt",
                table: "PrepaidCards");

            migrationBuilder.DropIndex(
                name: "IX_DiscountCodes_CreatedDateTime",
                table: "DiscountCodes");

            migrationBuilder.DropIndex(
                name: "IX_CustomerPhotos_IsApproved_CreatedAt",
                table: "CustomerPhotos");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPhotos_IsApproved",
                table: "CustomerPhotos",
                column: "IsApproved");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_CreatedAt",
                table: "QuoteRequests");
        }
    }
}
