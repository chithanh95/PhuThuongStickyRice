using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class DeactivateXoiNguSac : Migration
    {
        // User yêu cầu bỏ món Xôi ngũ sắc khỏi shop (giữ row để không mất
        // lịch sử đơn; có thể bật lại trong /products).
        private static readonly Guid XoiNguSacId = new Guid("a1b2c3d4-0005-4000-8000-000000000005");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiNguSacId,
                column: "Status",
                value: 2); // ProductStatus.Inactive = ngừng bán
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiNguSacId,
                column: "Status",
                value: 1); // ProductStatus.Active
        }
    }
}
