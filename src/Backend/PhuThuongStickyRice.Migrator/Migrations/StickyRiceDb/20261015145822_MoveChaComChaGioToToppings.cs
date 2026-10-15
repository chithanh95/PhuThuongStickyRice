using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class MoveChaComChaGioToToppings : Migration
    {
        // Chả cốm / chả giò chuyển từ product đặt riêng thành topping ăn kèm
        // (user yêu cầu). Giữ row product cũ ở trạng thái ngừng bán để không
        // mất lịch sử đơn; có thể bật lại trong /products.
        private static readonly Guid ChaComId = new Guid("b2c3d4e5-0001-4000-8000-000000000001");
        private static readonly Guid ChaGioId = new Guid("b2c3d4e5-0002-4000-8000-000000000002");
        private static readonly Guid ToppingChaComId = new Guid("c3d4e5f6-0001-4000-8000-000000000001");
        private static readonly Guid ToppingChaGioId = new Guid("c3d4e5f6-0002-4000-8000-000000000002");

        private static readonly DateTimeOffset SeedDate =
            new DateTimeOffset(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Toppings",
                columns: new[] { "Id", "Name", "ExtraPrice", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { ToppingChaComId, "Chả cốm", 15000m, 6, true, SeedDate },
                    { ToppingChaGioId, "Chả giò", 12000m, 7, true, SeedDate }
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: ChaComId,
                column: "Status",
                value: 2); // ProductStatus.Inactive

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: ChaGioId,
                column: "Status",
                value: 2); // ProductStatus.Inactive
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: ToppingChaComId);
            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: ToppingChaGioId);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: ChaComId,
                column: "Status",
                value: 1); // ProductStatus.Active

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: ChaGioId,
                column: "Status",
                value: 1); // ProductStatus.Active
        }
    }
}
