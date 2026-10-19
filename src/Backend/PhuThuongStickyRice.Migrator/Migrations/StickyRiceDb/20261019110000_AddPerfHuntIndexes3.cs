using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019110000_AddPerfHuntIndexes3")]
    public partial class AddPerfHuntIndexes3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: báo cáo hoàn tiền lọc theo RefundStatus
            // (RefundAppService.GetReportAsync), bảng Orders tăng không giới hạn.
            migrationBuilder.CreateIndex(
                name: "IX_Orders_RefundStatus",
                table: "Orders",
                column: "RefundStatus");

            // perf-hunt 2026-10-19: trang đối soát MoMo lọc
            // (PaymentMethod == MoMo && PaymentStatus == Paid) mỗi lần admin mở.
            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentMethod_PaymentStatus",
                table: "Orders",
                columns: new[] { "PaymentMethod", "PaymentStatus" });

            // perf-hunt 2026-10-19: trang admin xem audit log sort + count theo
            // CreatedDateTime; bảng tăng theo mỗi lần tạo/sửa/xóa sản phẩm.
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_CreatedDateTime",
                table: "AuditLogEntries",
                column: "CreatedDateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_RefundStatus",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentMethod_PaymentStatus",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_CreatedDateTime",
                table: "AuditLogEntries");
        }
    }
}
