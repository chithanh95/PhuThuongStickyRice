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
    [Migration("20261014120000_SeedWave1Catalog")]
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

            // Default toppings.
            migrationBuilder.Sql($@"
INSERT INTO Toppings (Id, Name, ExtraPrice, SortOrder, IsActive, CreatedDateTime) VALUES
('{ChaLuaId}', N'Chả lụa', 10000, 1, 1, '{SeededAt}'),
('{TrungOpLaId}', N'Trứng ốp la', 8000, 2, 1, '{SeededAt}'),
('{HanhPhiId}', N'Hành phi thêm', 5000, 3, 1, '{SeededAt}'),
('{RuocId}', N'Ruốc', 7000, 4, 1, '{SeededAt}'),
('{DuaGopId}', N'Dưa góp', 5000, 5, 1, '{SeededAt}');");

            // Size options: Nhỏ = base - 5000, Vừa = base, Lớn = base + 10000.
            InsertSizes(migrationBuilder, XoiGacId, 35000m);
            InsertSizes(migrationBuilder, XoiDoId, 25000m);
            InsertSizes(migrationBuilder, XoiXeoId, 25000m);
            InsertSizes(migrationBuilder, XoiVoId, 30000m);
            InsertSizes(migrationBuilder, XoiNguSacId, 40000m);

            // Family combos.
            migrationBuilder.Sql($@"
INSERT INTO Combos (Id, Name, Description, Price, IsActive, CreatedAt, CreatedDateTime) VALUES
('{ComboGiaDinhId}', N'Combo Gia Đình 4 Người', N'2 xôi gấc + 2 xôi xéo — đủ cho cả nhà 4 người, tiết kiệm hơn gọi lẻ.', 119000, 1, '2026-10-14T12:00:00', '{SeededAt}'),
('{ComboSumVayId}', N'Combo Xôi Ngũ Sắc Sum Vầy', N'2 xôi ngũ sắc + 1 xôi đỗ — mâm xôi rực rỡ cho buổi sum họp.', 99000, 1, '2026-10-14T12:00:00', '{SeededAt}');");

            migrationBuilder.Sql($@"
INSERT INTO ComboItems (Id, ComboId, ProductId, Quantity, CreatedDateTime) VALUES
(NEWID(), '{ComboGiaDinhId}', '{XoiGacId}', 2, '{SeededAt}'),
(NEWID(), '{ComboGiaDinhId}', '{XoiXeoId}', 2, '{SeededAt}'),
(NEWID(), '{ComboSumVayId}', '{XoiNguSacId}', 2, '{SeededAt}'),
(NEWID(), '{ComboSumVayId}', '{XoiDoId}', 1, '{SeededAt}');");

            // Dish-vote candidates.
            migrationBuilder.Sql($@"
INSERT INTO DishCandidates (Id, Name, Description, Votes, IsActive, CreatedAt, CreatedDateTime) VALUES
('{VoteComId}', N'Xôi cốm', N'Xôi cốm dẻo thơm mùa thu, ăn kèm dừa nạo và đậu xanh.', 0, 1, '2026-10-14T12:00:00', '{SeededAt}'),
('{VoteLacVungId}', N'Xôi lạc vừng', N'Xôi lạc bùi béo rắc vừng đen — món ăn dân dã khó quên.', 0, 1, '2026-10-14T12:00:00', '{SeededAt}'),
('{VoteSanId}', N'Xôi sắn mỡ hành', N'Xôi sắn dẻo quánh chan mỡ hành thơm lừng.', 0, 1, '2026-10-14T12:00:00', '{SeededAt}');");
        }

        private static void InsertSizes(MigrationBuilder migrationBuilder, Guid productId, decimal basePrice)
        {
            migrationBuilder.Sql($@"
INSERT INTO ProductSizeOptions (Id, ProductId, Name, Price, SortOrder, IsActive, CreatedDateTime) VALUES
(NEWID(), '{productId}', N'Nhỏ', {basePrice - 5000m}, 1, 1, '{SeededAt}'),
(NEWID(), '{productId}', N'Vừa', {basePrice}, 2, 1, '{SeededAt}'),
(NEWID(), '{productId}', N'Lớn', {basePrice + 10000m}, 3, 1, '{SeededAt}');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM DishCandidates WHERE Id IN ('{VoteComId}', '{VoteLacVungId}', '{VoteSanId}');
DELETE FROM ComboItems WHERE ComboId IN ('{ComboGiaDinhId}', '{ComboSumVayId}');
DELETE FROM Combos WHERE Id IN ('{ComboGiaDinhId}', '{ComboSumVayId}');
DELETE FROM Toppings WHERE Id IN ('{ChaLuaId}', '{TrungOpLaId}', '{HanhPhiId}', '{RuocId}');
DELETE FROM ProductSizeOptions WHERE Name IN (N'Nhỏ', N'Vừa', N'Lớn');");
        }
    }
}
