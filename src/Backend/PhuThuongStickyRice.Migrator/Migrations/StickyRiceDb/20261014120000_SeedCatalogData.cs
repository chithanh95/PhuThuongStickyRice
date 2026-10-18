using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Seeds the Wave 1 catalog: default toppings, size options for the five
    /// seeded xôi products, two family combos, dish-vote candidates, and
    /// catalog fields (tags / category / badges) for the seeded products.
    /// NOTE: hand-written migration without a Designer file, so all data
    /// operations use raw SQL — InsertData/UpdateData/DeleteData need the EF
    /// model to resolve column types and throw without it.
    /// </summary>
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261014120000_SeedCatalogData")]
    public partial class SeedCatalogData : Migration
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

        private const string SeededAt = "2026-10-14T12:00:00+00:00";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing shop-settings rows get the new default (45 minutes).
            migrationBuilder.Sql(
                "UPDATE ShopSettings SET EstimatedDeliveryMinutes = 45 WHERE EstimatedDeliveryMinutes = 0");

            // Catalog fields for the five seeded products.
            migrationBuilder.Sql($@"
UPDATE Products SET Tags = N'man', Category = N'Xôi mặn', IsSignature = 1 WHERE Id = '{XoiGacId}';
UPDATE Products SET Tags = N'chay', Category = N'Xôi mặn' WHERE Id = '{XoiDoId}';
UPDATE Products SET Tags = N'man', Category = N'Xôi mặn', AllergenInfo = N'Tôm (trong ruốc ăn kèm)' WHERE Id = '{XoiXeoId}';
UPDATE Products SET Tags = N'chay', Category = N'Xôi ngọt', IsNew = 1 WHERE Id = '{XoiVoId}';
UPDATE Products SET Tags = N'chay', Category = N'Xôi ngọt', AllergenInfo = N'Đậu phộng', IsSignature = 1 WHERE Id = '{XoiNguSacId}';");

            // Default toppings (bỏ qua nếu đã có topping cùng tên).
            migrationBuilder.Sql($@"
INSERT INTO Toppings (Id, Name, ExtraPrice, SortOrder, IsActive, CreatedDateTime)
SELECT '{ChaLuaId}', N'Chả lụa', 10000, 1, 1, '{SeededAt}' WHERE NOT EXISTS (SELECT 1 FROM Toppings WHERE Name = N'Chả lụa');
INSERT INTO Toppings (Id, Name, ExtraPrice, SortOrder, IsActive, CreatedDateTime)
SELECT '{TrungOpLaId}', N'Trứng ốp la', 8000, 2, 1, '{SeededAt}' WHERE NOT EXISTS (SELECT 1 FROM Toppings WHERE Name = N'Trứng ốp la');
INSERT INTO Toppings (Id, Name, ExtraPrice, SortOrder, IsActive, CreatedDateTime)
SELECT '{HanhPhiId}', N'Hành phi thêm', 5000, 3, 1, '{SeededAt}' WHERE NOT EXISTS (SELECT 1 FROM Toppings WHERE Name = N'Hành phi thêm');
INSERT INTO Toppings (Id, Name, ExtraPrice, SortOrder, IsActive, CreatedDateTime)
SELECT '{RuocId}', N'Ruốc', 7000, 4, 1, '{SeededAt}' WHERE NOT EXISTS (SELECT 1 FROM Toppings WHERE Name = N'Ruốc');
INSERT INTO Toppings (Id, Name, ExtraPrice, SortOrder, IsActive, CreatedDateTime)
SELECT '{DuaGopId}', N'Dưa góp', 5000, 5, 1, '{SeededAt}' WHERE NOT EXISTS (SELECT 1 FROM Toppings WHERE Name = N'Dưa góp');");

            // Size options: Nhỏ = base - 5000, Vừa = base, Lớn = base + 10000.
            InsertSizes(migrationBuilder, XoiGacId, 35000m);
            InsertSizes(migrationBuilder, XoiDoId, 25000m);
            InsertSizes(migrationBuilder, XoiXeoId, 25000m);
            InsertSizes(migrationBuilder, XoiVoId, 30000m);
            InsertSizes(migrationBuilder, XoiNguSacId, 40000m);

            // Family combos (bỏ qua nếu đã có combo cùng tên).
            migrationBuilder.Sql($@"
INSERT INTO Combos (Id, Name, Description, Price, IsActive, CreatedAt, CreatedDateTime)
SELECT '{ComboGiaDinhId}', N'Combo Gia Đình 4 Người', N'2 xôi gấc + 2 xôi xéo — đủ cho cả nhà 4 người, tiết kiệm hơn gọi lẻ.', 119000, 1, '2026-10-14T12:00:00', '{SeededAt}'
WHERE NOT EXISTS (SELECT 1 FROM Combos WHERE Name = N'Combo Gia Đình 4 Người');
INSERT INTO Combos (Id, Name, Description, Price, IsActive, CreatedAt, CreatedDateTime)
SELECT '{ComboSumVayId}', N'Combo Xôi Ngũ Sắc Sum Vầy', N'2 xôi ngũ sắc + 1 xôi đỗ — mâm xôi rực rỡ cho buổi sum họp.', 99000, 1, '2026-10-14T12:00:00', '{SeededAt}'
WHERE NOT EXISTS (SELECT 1 FROM Combos WHERE Name = N'Combo Xôi Ngũ Sắc Sum Vầy');");

            migrationBuilder.Sql($@"
INSERT INTO ComboItems (Id, ComboId, ProductId, Quantity, CreatedDateTime)
SELECT NEWID(), '{ComboGiaDinhId}', '{XoiGacId}', 2, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Combos WHERE Id = '{ComboGiaDinhId}') AND NOT EXISTS (SELECT 1 FROM ComboItems WHERE ComboId = '{ComboGiaDinhId}' AND ProductId = '{XoiGacId}');
INSERT INTO ComboItems (Id, ComboId, ProductId, Quantity, CreatedDateTime)
SELECT NEWID(), '{ComboGiaDinhId}', '{XoiXeoId}', 2, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Combos WHERE Id = '{ComboGiaDinhId}') AND NOT EXISTS (SELECT 1 FROM ComboItems WHERE ComboId = '{ComboGiaDinhId}' AND ProductId = '{XoiXeoId}');
INSERT INTO ComboItems (Id, ComboId, ProductId, Quantity, CreatedDateTime)
SELECT NEWID(), '{ComboSumVayId}', '{XoiNguSacId}', 2, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Combos WHERE Id = '{ComboSumVayId}') AND NOT EXISTS (SELECT 1 FROM ComboItems WHERE ComboId = '{ComboSumVayId}' AND ProductId = '{XoiNguSacId}');
INSERT INTO ComboItems (Id, ComboId, ProductId, Quantity, CreatedDateTime)
SELECT NEWID(), '{ComboSumVayId}', '{XoiDoId}', 1, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Combos WHERE Id = '{ComboSumVayId}') AND NOT EXISTS (SELECT 1 FROM ComboItems WHERE ComboId = '{ComboSumVayId}' AND ProductId = '{XoiDoId}');");

            // Dish-vote candidates: BỎ QUA — bảng DishCandidates đã bị migration
            // RemoveDishVoting (15/10) xóa, tính năng bình chọn món đã loại bỏ.
        }

        private static void InsertSizes(MigrationBuilder migrationBuilder, Guid productId, decimal basePrice)
        {
            // Bỏ qua nếu sản phẩm đã có size options.
            migrationBuilder.Sql($@"
INSERT INTO ProductSizeOptions (Id, ProductId, Name, Price, SortOrder, IsActive, CreatedDateTime)
SELECT NEWID(), '{productId}', N'Nhỏ', {basePrice - 5000m}, 1, 1, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Products WHERE Id = '{productId}') AND NOT EXISTS (SELECT 1 FROM ProductSizeOptions WHERE ProductId = '{productId}');
INSERT INTO ProductSizeOptions (Id, ProductId, Name, Price, SortOrder, IsActive, CreatedDateTime)
SELECT NEWID(), '{productId}', N'Vừa', {basePrice}, 2, 1, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Products WHERE Id = '{productId}') AND NOT EXISTS (SELECT 1 FROM ProductSizeOptions WHERE ProductId = '{productId}' AND Name = N'Vừa');
INSERT INTO ProductSizeOptions (Id, ProductId, Name, Price, SortOrder, IsActive, CreatedDateTime)
SELECT NEWID(), '{productId}', N'Lớn', {basePrice + 10000m}, 3, 1, '{SeededAt}'
WHERE EXISTS (SELECT 1 FROM Products WHERE Id = '{productId}') AND NOT EXISTS (SELECT 1 FROM ProductSizeOptions WHERE ProductId = '{productId}' AND Name = N'Lớn');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM ComboItems WHERE ComboId IN ('{ComboGiaDinhId}', '{ComboSumVayId}');
DELETE FROM Combos WHERE Id IN ('{ComboGiaDinhId}', '{ComboSumVayId}');
DELETE FROM Toppings WHERE Id IN ('{ChaLuaId}', '{TrungOpLaId}', '{HanhPhiId}', '{RuocId}');
DELETE FROM ProductSizeOptions WHERE Name IN (N'Nhỏ', N'Vừa', N'Lớn');");
        }
    }
}
