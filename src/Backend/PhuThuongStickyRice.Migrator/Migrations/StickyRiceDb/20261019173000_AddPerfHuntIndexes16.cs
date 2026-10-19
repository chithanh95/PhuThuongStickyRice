using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019173000_AddPerfHuntIndexes16")]
    public partial class AddPerfHuntIndexes16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 17:25): composite index cho hot path
            // đăng nhập. LoginAuditAppService.IsNewIpForUserAsync chạy trên
            // MỖI lần đăng nhập thành công (AuthController.AuditSuccessfulLoginAsync,
            // dùng chung cho Login thường và VerifyTwoFactor) với
            // WHERE UserName == x AND Success AND IpAddress == y.
            // Index đơn IX_LoginAudits_UserName chỉ seek được UserName rồi
            // lọc IpAddress trên toàn bộ lịch sử đăng nhập của user đó —
            // tài khoản admin/staff đăng nhập nhiều sẽ phình dần.
            // Composite (UserName, IpAddress) cho seek đủ 2 điều kiện bằng.
            // Non-unique, lookup-only, không đổi nghiệp vụ.
            migrationBuilder.CreateIndex(
                name: "IX_LoginAudits_UserName_IpAddress",
                table: "LoginAudits",
                columns: new[] { "UserName", "IpAddress" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoginAudits_UserName_IpAddress",
                table: "LoginAudits");
        }
    }
}
