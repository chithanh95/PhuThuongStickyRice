using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019144500_AddPerfHuntIndexes14")]
    public partial class AddPerfHuntIndexes14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 14:25): batch index còn thiếu từ scan
            // trực tiếp. Tất cả non-unique, lookup-only, không đổi nghiệp vụ.

            // (1) CustomerTags: VipPriorityAppService.GetVipsAsync chạy
            // WHERE TagName='VIP' + ORDER BY CreatedAt DESC mà KHÔNG Take
            // (admin list, bảng tăng theo mỗi khách VIP) và GetVipPhonesAsync
            // WHERE TagName='VIP' mỗi lần cache 5 phút hết hạn. Index đơn
            // CustomerPhone và unique (CustomerPhone, TagName) đều không
            // seek được theo TagName nên cả 2 query full scan + sort memory.
            migrationBuilder.CreateIndex(
                name: "IX_CustomerTags_TagName_CreatedAt",
                table: "CustomerTags",
                columns: new[] { "TagName", "CreatedAt" });

            // (2) ChatMessages: migration 20261019113600_AddPerfHuntIndexes6
            // đã tạo composite IX_ChatMessages_SessionId_CreatedAt nhưng
            // KHÔNG drop index đơn IX_ChatMessages_SessionId cũ và config
            // class vẫn khai báo index đơn (lệch model/DB). Composite bao phủ
            // hoàn toàn prefix SessionId nên index đơn là redundant — drop để
            // model khớp DB và giảm chi phí ghi.
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages");

            // (3) StaffChatMessages: entity chưa có config class nên bảng
            // không có index nào; StaffChatController.GetMessages chạy
            // ORDER BY SentAt DESC + Take(100) mỗi lần staff mở/poll kênh chat
            // (bảng tăng vô hạn, không retention) — sort toàn bảng.
            migrationBuilder.CreateIndex(
                name: "IX_StaffChatMessages_SentAt",
                table: "StaffChatMessages",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffChatMessages_SentAt",
                table: "StaffChatMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.DropIndex(
                name: "IX_CustomerTags_TagName_CreatedAt",
                table: "CustomerTags");
        }
    }
}
