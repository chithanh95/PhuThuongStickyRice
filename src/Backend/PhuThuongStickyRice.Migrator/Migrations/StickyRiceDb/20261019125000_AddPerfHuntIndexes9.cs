using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019125000_AddPerfHuntIndexes9")]
    public partial class AddPerfHuntIndexes9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 12:45): IX_Orders_CreatedAt.
            //
            // Chưa có index đơn nào trên Orders.CreatedAt — các index hiện có
            // đều là composite (Status,CreatedAt), (CustomerPhone,CreatedAt),
            // (PaymentMethod,PaymentStatus), không phục vụ được:
            // (1) admin order list (OrderAppService.GetListAsync:859) — query
            //     mặc định OrderByDescending(CreatedAt) KHÔNG lọc status =>
            //     full scan + sort toàn bộ bảng Orders (bảng nóng nhất) mỗi
            //     lần admin mở trang đơn / chuyển trang;
            // (2) QueueDisplay.GetNowServingAsync — filter CreatedAt theo ngày
            //     VN, bị TV /cho poll mỗi 15s;
            // (3) các báo cáo theo khoảng ngày không kèm status filter
            //     (OrderHourlyReport, GetSourceStatsAsync, GetTopProductsAsync).
            // Non-unique, lookup-only: không đổi nghiệp vụ.
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");
        }
    }
}
