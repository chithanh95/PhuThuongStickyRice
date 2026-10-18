using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261016153034_RemoveChaGioCompletely")]
    public partial class RemoveChaGioCompletely : Migration
    {
        private static readonly Guid ProductChaGioId = new Guid("b2c3d4e5-0002-4000-8000-000000000002");
        private static readonly Guid ToppingChaGioId = new Guid("c3d4e5f6-0002-4000-8000-000000000002");

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa topping Chả giò
            migrationBuilder.DeleteData(
                table: "Toppings",
                keyColumn: "Id",
                keyValue: ToppingChaGioId);

            // Xóa product Chả giò (nếu chưa có đơn hàng nào dùng thì xóa được)
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: ProductChaGioId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không khôi phục vì user yêu cầu xóa hẳn
        }
    }
}
