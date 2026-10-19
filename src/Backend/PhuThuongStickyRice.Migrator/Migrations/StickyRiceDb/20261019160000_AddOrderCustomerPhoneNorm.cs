using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019160000_AddOrderCustomerPhoneNorm")]
    public partial class AddOrderCustomerPhoneNorm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // perf-hunt 2026-10-19 (run 15:45): cột SĐT chuẩn hóa (9 số cuối,
            // chỉ chữ số) cho bảng Orders — các tra cứu khoan dung định dạng
            // trên hot path checkout (HasOrderedBefore, giới hạn đơn/ngày,
            // voucher đơn đầu, bonus giới thiệu, tra cứu đơn theo SĐT) đang
            // chạy LIKE '%...%' / EndsWith trên cột đã REPLACE → quét toàn
            // bảng Orders (bảng nóng nhất). Cột được tính 1 lần trong
            // constructor của Order nên mọi đơn mới đều đúng; backfill bên
            // dưới cho đơn cũ. Index non-unique, lookup-only.

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneNorm",
                table: "Orders",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: false,
                defaultValue: "");

            // Backfill: strip các ký tự phân tách thường gặp rồi lấy 9 ký tự
            // cuối — cùng ngữ nghĩa với Order.ComputePhoneNorm cho dữ liệu
            // thực tế (SĐT VN chỉ chứa số, khoảng trắng, +, -, ., (, )).
            // RIGHT(x, 9) giữ nguyên chuỗi ngắn hơn 9 ký tự.
            migrationBuilder.Sql(@"
UPDATE [Orders]
SET [CustomerPhoneNorm] = RIGHT(
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([CustomerPhone], ' ', ''), '-', ''), '.', ''), '(', ''), ')', ''), '+', ''), '/', ''),
    9)
WHERE [CustomerPhoneNorm] = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerPhoneNorm",
                table: "Orders",
                column: "CustomerPhoneNorm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerPhoneNorm",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneNorm",
                table: "Orders");
        }
    }
}
