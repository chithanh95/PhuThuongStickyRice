using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019113000_AddPerfHuntIndexes5")]
    public partial class AddPerfHuntIndexes5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: WeeklyPlanAppService.GenerateOrderCodeAsync
            // kiểm tra `OrderCode == code` (AnyAsync, retry tối đa 10 lần) mỗi khi
            // tạo đơn gói tuần — bảng chưa có index nào trên OrderCode nên mỗi lần
            // check là một full scan. Index non-unique: chỉ tăng tốc lookup, không
            // đổi hành vi khi trùng mã (vòng retry trong code giữ nguyên).
            migrationBuilder.CreateIndex(
                name: "IX_WeeklyPlanOrders_OrderCode",
                table: "WeeklyPlanOrders",
                column: "OrderCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyPlanOrders_OrderCode",
                table: "WeeklyPlanOrders");
        }
    }
}
