using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Seeds the Wave 1 catalog: default toppings, size options for the five
    /// seeded xôi products, two family combos, dish-vote candidates, and
    /// catalog fields (tags / category / badges) for the seeded products.
    /// </summary>
    public partial class SeedWave1Catalog : Migration
    {
        private static readonly Guid XoiGacId = new Guid("a1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid XoiDoId = new Guid("a1b2c3d4-0002-4000-8000-000000000002");
        private static readonly Guid XoiXeoId = new Guid("a1b2c3d4-0003-4000-8000-000000000003");
        private static readonly Guid XoiVoId = new Guid("a1b2c3d4-0004-4000-8000-000000000004");
        private static readonly Guid XoiNguSacId = new Guid("a1b2c3d4-0005-4000-8000-000000000005");

        private static readonly Guid ChaLuaId = new Guid("b1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid TrungOpLaId = new Guid("b1b2c3d4-0002-4000-8000-000000000002");
        private static readonly Guid HanhPhiId = new Guid("b1b2c3d4-0003-4000-8000-000000000003");
        private static readonly Guid RuocId = new Guid("b1b2c3d4-0004-4000-8000-000000000004");
        private static readonly Guid DuaGopId = new Guid("b1b2c3d4-0005-4000-8000-000000000005");

        private static readonly Guid ComboGiaDinhId = new Guid("c1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid ComboSumVayId = new Guid("c1b2c3d4-0002-4000-8000-000000000002");

        private static readonly Guid VoteComId = new Guid("d1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid VoteLacVungId = new Guid("d1b2c3d4-0002-4000-8000-000000000002");
        private static readonly Guid VoteSanId = new Guid("d1b2c3d4-0003-4000-8000-000000000003");

        private static readonly DateTimeOffset SeededAt =
            new DateTimeOffset(new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing shop-settings rows get the new default (45 minutes).
            migrationBuilder.Sql(
                "UPDATE ShopSettings SET EstimatedDeliveryMinutes = 45 WHERE EstimatedDeliveryMinutes = 0");

            // Catalog fields for the five seeded products.
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiGacId,
                columns: new[] { "Tags", "Category", "IsSignature" },
                values: new object[] { "man", "Xôi mặn", true });
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiDoId,
                columns: new[] { "Tags", "Category" },
                values: new object[] { "chay", "Xôi mặn" });
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiXeoId,
                columns: new[] { "Tags", "Category", "AllergenInfo" },
                values: new object[] { "man", "Xôi mặn", "Tôm (trong ruốc ăn kèm)" });
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiVoId,
                columns: new[] { "Tags", "Category", "IsNew" },
                values: new object[] { "chay", "Xôi ngọt", true });
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: XoiNguSacId,
                columns: new[] { "Tags", "Category", "AllergenInfo", "IsSignature" },
                values: new object[] { "chay", "Xôi ngọt", "Đậu phộng", true });

            // Default toppings.
            migrationBuilder.InsertData(
                table: "Toppings",
                columns: new[] { "Id", "Name", "ExtraPrice", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { ChaLuaId, "Chả lụa", 10000m, 1, true, SeededAt },
                    { TrungOpLaId, "Trứng ốp la", 8000m, 2, true, SeededAt },
                    { HanhPhiId, "Hành phi thêm", 5000m, 3, true, SeededAt },
                    { RuocId, "Ruốc", 7000m, 4, true, SeededAt },
                    { DuaGopId, "Dưa góp", 5000m, 5, true, SeededAt }
                });

            // Size options: Nhỏ = base - 5000, Vừa = base, Lớn = base + 10000.
            InsertSizes(migrationBuilder, XoiGacId, 35000m);
            InsertSizes(migrationBuilder, XoiDoId, 25000m);
            InsertSizes(migrationBuilder, XoiXeoId, 25000m);
            InsertSizes(migrationBuilder, XoiVoId, 30000m);
            InsertSizes(migrationBuilder, XoiNguSacId, 40000m);

            // Family combos.
            migrationBuilder.InsertData(
                table: "Combos",
                columns: new[] { "Id", "Name", "Description", "Price", "IsActive", "CreatedAt", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        ComboGiaDinhId, "Combo Gia Đình 4 Người",
                        "2 xôi gấc + 2 xôi xéo — đủ cho cả nhà 4 người, tiết kiệm hơn gọi lẻ.",
                        119000m, true,
                        new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), SeededAt
                    },
                    {
                        ComboSumVayId, "Combo Xôi Ngũ Sắc Sum Vầy",
                        "2 xôi ngũ sắc + 1 xôi đỗ — mâm xôi rực rỡ cho buổi sum họp.",
                        99000m, true,
                        new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), SeededAt
                    }
                });

            migrationBuilder.InsertData(
                table: "ComboItems",
                columns: new[] { "Id", "ComboId", "ProductId", "Quantity", "CreatedDateTime" },
                values: new object[,]
                {
                    { Guid.NewGuid(), ComboGiaDinhId, XoiGacId, 2, SeededAt },
                    { Guid.NewGuid(), ComboGiaDinhId, XoiXeoId, 2, SeededAt },
                    { Guid.NewGuid(), ComboSumVayId, XoiNguSacId, 2, SeededAt },
                    { Guid.NewGuid(), ComboSumVayId, XoiDoId, 1, SeededAt }
                });

            // Dish-vote candidates.
            migrationBuilder.InsertData(
                table: "DishCandidates",
                columns: new[] { "Id", "Name", "Description", "Votes", "IsActive", "CreatedAt", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        VoteComId, "Xôi cốm",
                        "Xôi cốm dẻo thơm mùa thu, ăn kèm dừa nạo và đậu xanh.",
                        0L, true,
                        new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), SeededAt
                    },
                    {
                        VoteLacVungId, "Xôi lạc vừng",
                        "Xôi lạc bùi béo rắc vừng đen — món ăn dân dã khó quên.",
                        0L, true,
                        new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), SeededAt
                    },
                    {
                        VoteSanId, "Xôi sắn mỡ hành",
                        "Xôi sắn dẻo quánh chan mỡ hành thơm lừng.",
                        0L, true,
                        new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc), SeededAt
                    }
                });
        }

        private static void InsertSizes(MigrationBuilder migrationBuilder, Guid productId, decimal basePrice)
        {
            migrationBuilder.InsertData(
                table: "ProductSizeOptions",
                columns: new[] { "Id", "ProductId", "Name", "Price", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { Guid.NewGuid(), productId, "Nhỏ", basePrice - 5000m, 1, true, SeededAt },
                    { Guid.NewGuid(), productId, "Vừa", basePrice, 2, true, SeededAt },
                    { Guid.NewGuid(), productId, "Lớn", basePrice + 10000m, 3, true, SeededAt }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "DishCandidates", keyColumn: "Id", keyValue: VoteComId);
            migrationBuilder.DeleteData(table: "DishCandidates", keyColumn: "Id", keyValue: VoteLacVungId);
            migrationBuilder.DeleteData(table: "DishCandidates", keyColumn: "Id", keyValue: VoteSanId);

            migrationBuilder.DeleteData(table: "Combos", keyColumn: "Id", keyValue: ComboGiaDinhId);
            migrationBuilder.DeleteData(table: "Combos", keyColumn: "Id", keyValue: ComboSumVayId);

            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: ChaLuaId);
            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: TrungOpLaId);
            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: HanhPhiId);
            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: RuocId);
            migrationBuilder.DeleteData(table: "Toppings", keyColumn: "Id", keyValue: DuaGopId);

            migrationBuilder.Sql("DELETE FROM ProductSizeOptions WHERE Name IN (N'Nhỏ', N'Vừa', N'Lớn')");
        }
    }
}
