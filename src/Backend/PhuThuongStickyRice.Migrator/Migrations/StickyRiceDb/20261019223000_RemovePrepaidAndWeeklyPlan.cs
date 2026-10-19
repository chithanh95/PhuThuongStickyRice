using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Xóa tính năng gói trả trước (thẻ xôi + gói tuần) theo yêu cầu:
    /// không hợp với mô hình quán. Xóa bảng PrepaidCards, WeeklyPlans,
    /// WeeklyPlanOrders (theo thứ tự FK).
    /// </summary>
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019223000_RemovePrepaidAndWeeklyPlan")]
    public partial class RemovePrepaidAndWeeklyPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: IF EXISTS để không gãy nếu bảng đã bị xóa thủ công.
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'dbo.WeeklyPlanOrders', N'U') IS NOT NULL DROP TABLE [dbo].[WeeklyPlanOrders];");
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'dbo.WeeklyPlans', N'U') IS NOT NULL DROP TABLE [dbo].[WeeklyPlans];");
            migrationBuilder.Sql(
                "IF OBJECT_ID(N'dbo.PrepaidCards', N'U') IS NOT NULL DROP TABLE [dbo].[PrepaidCards];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không khôi phục: bảng đã xóa theo yêu cầu nghiệp vụ.
        }
    }
}
