using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019180000_AddPerfHuntIndexes18")]
    public partial class AddPerfHuntIndexes18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: composite index lookup-only, non-unique,
            // không đổi nghiệp vụ.
            //
            // (1) Orders(CustomerPhoneNorm, Status): LoyaltyAppService.
            // HasNoPriorCompletedOrderAsync (chạy mỗi khi đơn dùng mã giới
            // thiệu hoàn thành) lọc CustomerPhoneNorm == 9 số cuối AND
            // Status == Completed. Trước đây chỉ có index đơn CustomerPhoneNorm
            // nên phải seek theo SĐT rồi lọc Status trên toàn bộ lịch sử đơn
            // của khách đó; composite index cho full seek cả 2 điều kiện.
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerPhoneNorm_Status",
                table: "Orders",
                columns: new[] { "CustomerPhoneNorm", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerPhoneNorm_Status",
                table: "Orders");
        }
    }
}
