using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019174500_AddPerfHuntIndexes17")]
    public partial class AddPerfHuntIndexes17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 17:25): batch index từ audit subagent.
            // Tất cả non-unique, lookup-only, không đổi nghiệp vụ.

            // (1) OrderLines: EF Core KHÔNG tự tạo index cho FK (khác EF6)
            // nên cột OrderId chưa có index nào. Mọi báo cáo join lines qua
            // navigation (Dashboard/GetTopProductsAsync, GetSoldQuantitiesAsync,
            // GetRevenueSummaryAsync; GrossProfit/Profit/Hourly/Forecast report;
            // export Excel WHERE OrderId IN (...); CustomerDetail top-spent
            // Where(l => ids.Contains(l.OrderId))) đều lookup theo OrderId.
            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                table: "OrderLines",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderLines_OrderId",
                table: "OrderLines");
        }
    }
}
