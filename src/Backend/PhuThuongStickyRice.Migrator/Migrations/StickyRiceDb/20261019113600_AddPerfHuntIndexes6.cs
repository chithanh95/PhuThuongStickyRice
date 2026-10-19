using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019113600_AddPerfHuntIndexes6")]
    public partial class AddPerfHuntIndexes6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: 2 endpoint public bị thiếu composite index:
            // (1) GET last-order-items (LandingPublicController.GetLastOrderItems):
            //     Where CustomerPhone (equality) + OrderByDescending CreatedAt + Take 1.
            //     Index (CustomerPhone, Status) hiện có seek được phone nhưng sort
            //     trong memory. Composite (CustomerPhone, CreatedAt) cho seek + sort
            //     trực tiếp trên index.
            // (2) GET chat/{sessionId} (LandingPublicController.GetChatMessages):
            //     Where SessionId + OrderBy CreatedAt + Take 50. Index hiện chỉ có
            //     SessionId đơn. Composite (SessionId, CreatedAt) phục vụ cả filter
            //     lẫn sort.
            // Index non-unique: chỉ tăng tốc lookup, không đổi nghiệp vụ.
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerPhone_CreatedAt",
                table: "Orders",
                columns: new[] { "CustomerPhone", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "SessionId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SessionId_CreatedAt",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerPhone_CreatedAt",
                table: "Orders");
        }
    }
}
