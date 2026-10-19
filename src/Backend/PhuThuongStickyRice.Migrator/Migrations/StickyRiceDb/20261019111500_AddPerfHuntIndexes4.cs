using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019111500_AddPerfHuntIndexes4")]
    public partial class AddPerfHuntIndexes4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19: trang admin xem login audit sort +
            // LongCount theo At mỗi lần mở; trước đây chỉ có index UserName.
            migrationBuilder.CreateIndex(
                name: "IX_LoginAudits_At",
                table: "LoginAudits",
                column: "At");

            // perf-hunt 2026-10-19: landing GetReviews (public, cache 5 phút)
            // lọc !IsHidden + sort CreatedAt desc mỗi lần cache-miss.
            migrationBuilder.CreateIndex(
                name: "IX_OrderReviews_IsHidden_CreatedAt",
                table: "OrderReviews",
                columns: new[] { "IsHidden", "CreatedAt" });

            // perf-hunt 2026-10-19: admin lọc ticket theo Status + sort
            // CreatedAt desc (SupportTicketAppService.GetListAsync).
            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status_CreatedAt",
                table: "SupportTickets",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoginAudits_At",
                table: "LoginAudits");

            migrationBuilder.DropIndex(
                name: "IX_OrderReviews_IsHidden_CreatedAt",
                table: "OrderReviews");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_Status_CreatedAt",
                table: "SupportTickets");
        }
    }
}
