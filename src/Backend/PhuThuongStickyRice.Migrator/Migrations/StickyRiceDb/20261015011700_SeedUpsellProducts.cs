using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class SeedUpsellProducts : Migration
    {
        private static readonly Guid ChaComId = new Guid("b2c3d4e5-0001-4000-8000-000000000001");
        private static readonly Guid ChaGioId = new Guid("b2c3d4e5-0002-4000-8000-000000000002");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PausedReason",
                table: "ShopSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Upsell side dishes for the "Ăn kèm ngon hơn với" popup.
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "Name", "Description", "Price", "Currency", "Status", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        ChaComId, "CHA-COM", "Chả cốm",
                        "Chả cốm dẻo thơm mùi cốm non làng Vòng, chiên vàng giòn — ăn kèm xôi cực hợp.",
                        15000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    },
                    {
                        ChaGioId, "CHA-GIO", "Chả giò",
                        "Chả giò giòn rụm nhân thịt mộc nhĩ, chấm tương ớt — món ăn kèm khoái khẩu với xôi.",
                        12000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: ChaComId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: ChaGioId);

            migrationBuilder.DropColumn(
                name: "PausedReason",
                table: "ShopSettings");
        }
    }
}
