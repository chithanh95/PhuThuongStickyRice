using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// bug-hunt 2026-10-19 (run 15:35): thêm cột IdempotencyKey (nullable) +
    /// unique index cho WalletTransactions — khóa chống hoàn tiền ví trùng.
    /// WalletAppService.RefundAsync cũ check AnyAsync(Phone, Reason) rồi mới
    /// cộng tiền: 2 lần hủy đơn đồng thời (khách + staff, webhook gọi lặp)
    /// cùng qua bước check -> cộng tiền 2 lần, chỉ ghi được 1-2 dòng log.
    /// Bản mới insert dòng guard với IdempotencyKey trước khi cộng tiền;
    /// unique index làm trọng tài nguyên tử. Không dùng unique (Phone, Reason)
    /// vì các lần charge hợp lệ lặp lại cùng lý do "Thanh toán đơn {mã đơn}"
    /// (khách sửa đơn thêm món nhiều lần) sẽ bị chặn oan. NULL không va chạm
    /// trong unique index nên các dòng charge/nạp/trừ tay (không đặt khóa)
    /// không bị ảnh hưởng.
    /// </summary>
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019155000_AddWalletTransactionIdempotencyKey")]
    public partial class AddWalletTransactionIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WalletTransactions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_IdempotencyKey",
                table: "WalletTransactions",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_IdempotencyKey",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WalletTransactions");
        }
    }
}
