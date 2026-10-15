using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class UpdateUniformPricing : Migration
    {
        // Mô hình giá đồng nhất (user chốt 2026-10-15):
        // - Món suất: giá gốc 15k, 3 cỡ gói (Gói nhỏ 15k / Gói vừa 25k / Gói lớn 35k).
        // - Món kg: 50k/kg (gói 500g = 25k, gói 1kg = 50k).
        // - Thêm mới: XOI-LAC (suất) + XOI-DO-KG, XOI-VO-KG, XOI-LAC-KG.

        private static readonly Guid XoiGacId = new Guid("a1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid XoiDoId = new Guid("a1b2c3d4-0002-4000-8000-000000000002");
        private static readonly Guid XoiXeoId = new Guid("a1b2c3d4-0003-4000-8000-000000000003");
        private static readonly Guid XoiVoId = new Guid("a1b2c3d4-0004-4000-8000-000000000004");
        private static readonly Guid XoiLacId = new Guid("d4e5f6a7-0001-4000-8000-000000000001");

        private static readonly Guid XoiGacKgId = new Guid("b1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid XoiXeoKgId = new Guid("b1b2c3d4-0002-4000-8000-000000000002");
        private static readonly Guid XoiDoKgId = new Guid("d4e5f6a7-0002-4000-8000-000000000002");
        private static readonly Guid XoiVoKgId = new Guid("d4e5f6a7-0003-4000-8000-000000000003");
        private static readonly Guid XoiLacKgId = new Guid("d4e5f6a7-0004-4000-8000-000000000004");

        // Size options cũ của món kg (để update giá).
        private static readonly Guid GacKg500gId = new Guid("b1b2c3d4-0011-4000-8000-000000000001");
        private static readonly Guid GacKg1KgId = new Guid("b1b2c3d4-0012-4000-8000-000000000001");
        private static readonly Guid XeoKg500gId = new Guid("b1b2c3d4-0011-4000-8000-000000000002");
        private static readonly Guid XeoKg1KgId = new Guid("b1b2c3d4-0012-4000-8000-000000000002");

        private static readonly DateTimeOffset SeedDate =
            new DateTimeOffset(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Giá gốc món suất -> 15k.
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-GAC", column: "Price", value: 15000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-DO", column: "Price", value: 15000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-XEO", column: "Price", value: 15000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-VO", column: "Price", value: 15000m);

            // 2. Xóa size options cũ (Nhỏ/Vừa/Lớn, Guid random) của 4 món suất.
            migrationBuilder.Sql(
                "DELETE FROM ProductSizeOptions WHERE ProductId IN (SELECT Id FROM Products WHERE Code IN (N'XOI-GAC', N'XOI-DO', N'XOI-XEO', N'XOI-VO'))");

            // 3. Thêm mới XOI-LAC (suất, 15k).
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "Name", "Description", "Price", "Currency", "Status", "CreatedDateTime", "CostPrice", "IsNew", "IsSignature", "Unit" },
                values: new object[,]
                {
                    {
                        XoiLacId, "XOI-LAC", "Xôi lạc",
                        "Xôi nếp dẻo thơm nấu cùng lạc bùi béo — món ăn dân dã của làng nghề Phú Thượng.",
                        15000m, "VND", 1, SeedDate, 10000m, false, false, "suất"
                    }
                });

            // 4. Size options mới cho 5 món suất: Gói nhỏ 15k / Gói vừa 25k / Gói lớn 35k.
            migrationBuilder.InsertData(
                table: "ProductSizeOptions",
                columns: new[] { "Id", "ProductId", "Name", "Price", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0011-4000-8000-000000000001"), XoiGacId, "Gói nhỏ", 15000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0012-4000-8000-000000000001"), XoiGacId, "Gói vừa", 25000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0013-4000-8000-000000000001"), XoiGacId, "Gói lớn", 35000m, 2, true, SeedDate },
                    { new Guid("d4e5f6a7-0021-4000-8000-000000000001"), XoiDoId, "Gói nhỏ", 15000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0022-4000-8000-000000000001"), XoiDoId, "Gói vừa", 25000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0023-4000-8000-000000000001"), XoiDoId, "Gói lớn", 35000m, 2, true, SeedDate },
                    { new Guid("d4e5f6a7-0031-4000-8000-000000000001"), XoiXeoId, "Gói nhỏ", 15000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0032-4000-8000-000000000001"), XoiXeoId, "Gói vừa", 25000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0033-4000-8000-000000000001"), XoiXeoId, "Gói lớn", 35000m, 2, true, SeedDate },
                    { new Guid("d4e5f6a7-0041-4000-8000-000000000001"), XoiVoId, "Gói nhỏ", 15000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0042-4000-8000-000000000001"), XoiVoId, "Gói vừa", 25000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0043-4000-8000-000000000001"), XoiVoId, "Gói lớn", 35000m, 2, true, SeedDate },
                    { new Guid("d4e5f6a7-0051-4000-8000-000000000001"), XoiLacId, "Gói nhỏ", 15000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0052-4000-8000-000000000001"), XoiLacId, "Gói vừa", 25000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0053-4000-8000-000000000001"), XoiLacId, "Gói lớn", 35000m, 2, true, SeedDate }
                });

            // 5. Món kg hiện có -> 50k/kg.
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Id", keyValue: XoiGacKgId, column: "Price", value: 50000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Id", keyValue: XoiXeoKgId, column: "Price", value: 50000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: GacKg500gId, column: "Price", value: 25000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: GacKg1KgId, column: "Price", value: 50000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: XeoKg500gId, column: "Price", value: 25000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: XeoKg1KgId, column: "Price", value: 50000m);

            // 6. Thêm mới 3 món đóng gói theo kg.
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "Name", "Description", "Price", "Currency", "Status", "CreatedDateTime", "CostPrice", "IsNew", "IsSignature", "Unit" },
                values: new object[,]
                {
                    {
                        XoiDoKgId, "XOI-DO-KG", "Xôi đỗ đóng gói",
                        "Xôi đỗ đóng gói theo cân cho gia đình — giá theo kg, chọn gói 500g hoặc 1kg.",
                        50000m, "VND", 1, SeedDate, 30000m, false, false, "kg"
                    },
                    {
                        XoiVoKgId, "XOI-VO-KG", "Xôi vò đóng gói",
                        "Xôi vò đóng gói theo cân, tơi từng hạt — giá theo kg, chọn gói 500g hoặc 1kg.",
                        50000m, "VND", 1, SeedDate, 30000m, false, false, "kg"
                    },
                    {
                        XoiLacKgId, "XOI-LAC-KG", "Xôi lạc đóng gói",
                        "Xôi lạc đóng gói theo cân, bùi béo — giá theo kg, chọn gói 500g hoặc 1kg.",
                        50000m, "VND", 1, SeedDate, 30000m, false, false, "kg"
                    }
                });

            migrationBuilder.InsertData(
                table: "ProductSizeOptions",
                columns: new[] { "Id", "ProductId", "Name", "Price", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { new Guid("d4e5f6a7-0061-4000-8000-000000000001"), XoiDoKgId, "500g", 25000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0062-4000-8000-000000000001"), XoiDoKgId, "1kg", 50000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0071-4000-8000-000000000001"), XoiVoKgId, "500g", 25000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0072-4000-8000-000000000001"), XoiVoKgId, "1kg", 50000m, 1, true, SeedDate },
                    { new Guid("d4e5f6a7-0081-4000-8000-000000000001"), XoiLacKgId, "500g", 25000m, 0, true, SeedDate },
                    { new Guid("d4e5f6a7-0082-4000-8000-000000000001"), XoiLacKgId, "1kg", 50000m, 1, true, SeedDate }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa size + product kg mới.
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0061-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0062-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0071-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0072-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0081-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0082-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiDoKgId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiVoKgId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiLacKgId);

            // Khôi phục giá món kg cũ.
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Id", keyValue: XoiGacKgId, column: "Price", value: 120000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Id", keyValue: XoiXeoKgId, column: "Price", value: 100000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: GacKg500gId, column: "Price", value: 60000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: GacKg1KgId, column: "Price", value: 120000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: XeoKg500gId, column: "Price", value: 50000m);
            migrationBuilder.UpdateData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: XeoKg1KgId, column: "Price", value: 100000m);

            // Xóa size gói mới + XOI-LAC.
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0011-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0012-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0013-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0021-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0022-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0023-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0031-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0032-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0033-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0041-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0042-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0043-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0051-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0052-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("d4e5f6a7-0053-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiLacId);

            // Khôi phục size Nhỏ/Vừa/Lớn cũ cho 4 món suất.
            migrationBuilder.InsertData(
                table: "ProductSizeOptions",
                columns: new[] { "Id", "ProductId", "Name", "Price", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { new Guid("e5f6a7b8-0011-4000-8000-000000000001"), XoiGacId, "Nhỏ", 30000m, 1, true, SeedDate },
                    { new Guid("e5f6a7b8-0012-4000-8000-000000000001"), XoiGacId, "Vừa", 35000m, 2, true, SeedDate },
                    { new Guid("e5f6a7b8-0013-4000-8000-000000000001"), XoiGacId, "Lớn", 45000m, 3, true, SeedDate },
                    { new Guid("e5f6a7b8-0021-4000-8000-000000000001"), XoiDoId, "Nhỏ", 20000m, 1, true, SeedDate },
                    { new Guid("e5f6a7b8-0022-4000-8000-000000000001"), XoiDoId, "Vừa", 25000m, 2, true, SeedDate },
                    { new Guid("e5f6a7b8-0023-4000-8000-000000000001"), XoiDoId, "Lớn", 35000m, 3, true, SeedDate },
                    { new Guid("e5f6a7b8-0031-4000-8000-000000000001"), XoiXeoId, "Nhỏ", 20000m, 1, true, SeedDate },
                    { new Guid("e5f6a7b8-0032-4000-8000-000000000001"), XoiXeoId, "Vừa", 25000m, 2, true, SeedDate },
                    { new Guid("e5f6a7b8-0033-4000-8000-000000000001"), XoiXeoId, "Lớn", 35000m, 3, true, SeedDate },
                    { new Guid("e5f6a7b8-0041-4000-8000-000000000001"), XoiVoId, "Nhỏ", 25000m, 1, true, SeedDate },
                    { new Guid("e5f6a7b8-0042-4000-8000-000000000001"), XoiVoId, "Vừa", 30000m, 2, true, SeedDate },
                    { new Guid("e5f6a7b8-0043-4000-8000-000000000001"), XoiVoId, "Lớn", 40000m, 3, true, SeedDate }
                });

            // Khôi phục giá gốc cũ.
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-GAC", column: "Price", value: 35000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-DO", column: "Price", value: 25000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-XEO", column: "Price", value: 25000m);
            migrationBuilder.UpdateData(table: "Products", keyColumn: "Code", keyValue: "XOI-VO", column: "Price", value: 30000m);
        }
    }
}
