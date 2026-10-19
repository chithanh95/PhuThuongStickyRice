using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// bug-hunt 2026-10-19 (run 15:25): thêm cột FacebookAppSecret cho
    /// ShopSettings — App Secret dùng để verify chữ ký X-Hub-Signature-256
    /// của Messenger webhook POST, chống giả mạo callback tạo đơn ảo.
    /// Nullable: shop chưa cấu hình thì webhook chạy như cũ (log cảnh báo).
    /// </summary>
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019153600_AddFacebookAppSecret")]
    public partial class AddFacebookAppSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookAppSecret",
                table: "ShopSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookAppSecret",
                table: "ShopSettings");
        }
    }
}
