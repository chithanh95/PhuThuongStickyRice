using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class SeedXoiProducts : Migration
    {
        private static readonly Guid XoiGacId = new Guid("a1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid XoiDoId = new Guid("a1b2c3d4-0002-4000-8000-000000000002");
        private static readonly Guid XoiXeoId = new Guid("a1b2c3d4-0003-4000-8000-000000000003");
        private static readonly Guid XoiVoId = new Guid("a1b2c3d4-0004-4000-8000-000000000004");
        private static readonly Guid XoiNguSacId = new Guid("a1b2c3d4-0005-4000-8000-000000000005");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "Name", "Description", "Price", "Currency", "Status", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        XoiGacId, "XOI-GAC", "Xôi gấc",
                        "Xôi gấc truyền thống nấu từ thịt gấc và gạo nếp cái hoa vàng, màu đỏ đẹp mắt, béo ngậy.",
                        35000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    },
                    {
                        XoiDoId, "XOI-DO", "Xôi đỗ",
                        "Xôi đỗ xanh bùi béo nấu từ đậu xanh và gạo nếp, hương vị đặc trưng làng nghề Phú Thượng.",
                        25000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    },
                    {
                        XoiXeoId, "XOI-XEO", "Xôi xéo",
                        "Xôi xéo vàng óng ả, ăn kèm hành phi giòn rụm và đậu xanh mịn.",
                        25000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    },
                    {
                        XoiVoId, "XOI-VO", "Xôi vò",
                        "Xôi vò tơi từng hạt, hòa quyện đậu xanh, thơm ngon khó cưỡng.",
                        30000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    },
                    {
                        XoiNguSacId, "XOI-NS", "Xôi ngũ sắc",
                        "Xôi ngũ sắc rực rỡ từ nhiều loại đậu và gạo nếp, hương vị phong phú, đẹp mắt.",
                        40000m, "VND", 1,
                        new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiGacId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiDoId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiXeoId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiVoId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiNguSacId);
        }
    }
}
