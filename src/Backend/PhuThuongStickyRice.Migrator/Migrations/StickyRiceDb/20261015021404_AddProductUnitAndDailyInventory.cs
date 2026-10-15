using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class AddProductUnitAndDailyInventory : Migration
    {
        // Item 81: seed products sold by weight (giá theo kg, bán gói 500g/1kg).
        private static readonly Guid XoiGacKgId = new Guid("b1b2c3d4-0001-4000-8000-000000000001");
        private static readonly Guid XoiXeoKgId = new Guid("b1b2c3d4-0002-4000-8000-000000000002");

        private static readonly DateTimeOffset SeedDate =
            new DateTimeOffset(new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "suất");

            migrationBuilder.CreateTable(
                name: "DailyInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RemainingQty = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyInventories_Date_ProductId",
                table: "DailyInventories",
                columns: new[] { "Date", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyInventories_ProductId",
                table: "DailyInventories",
                column: "ProductId");

            // Item 81: two products sold by weight — price is per 1 kg,
            // sold as 500g / 1kg packs via size options.
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Code", "Name", "Description", "Price", "Currency", "Status", "CreatedDateTime", "CostPrice", "IsNew", "IsSignature", "Unit" },
                values: new object[,]
                {
                    {
                        XoiGacKgId, "XOI-GAC-KG", "Xôi gấc đóng gói",
                        "Xôi gấc đóng gói theo cân cho gia đình, cúng giỗ — giá theo kg, chọn gói 500g hoặc 1kg.",
                        120000m, "VND", 1, SeedDate, 70000m, false, false, "kg"
                    },
                    {
                        XoiXeoKgId, "XOI-XEO-KG", "Xôi xéo đóng gói",
                        "Xôi xéo đóng gói theo cân, kèm hành phi và đậu xanh — giá theo kg, chọn gói 500g hoặc 1kg.",
                        100000m, "VND", 1, SeedDate, 60000m, false, false, "kg"
                    }
                });

            migrationBuilder.InsertData(
                table: "ProductSizeOptions",
                columns: new[] { "Id", "ProductId", "Name", "Price", "SortOrder", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    { new Guid("b1b2c3d4-0011-4000-8000-000000000001"), XoiGacKgId, "500g", 60000m, 0, true, SeedDate },
                    { new Guid("b1b2c3d4-0012-4000-8000-000000000001"), XoiGacKgId, "1kg", 120000m, 1, true, SeedDate },
                    { new Guid("b1b2c3d4-0011-4000-8000-000000000002"), XoiXeoKgId, "500g", 50000m, 0, true, SeedDate },
                    { new Guid("b1b2c3d4-0012-4000-8000-000000000002"), XoiXeoKgId, "1kg", 100000m, 1, true, SeedDate }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("b1b2c3d4-0011-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("b1b2c3d4-0012-4000-8000-000000000001"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("b1b2c3d4-0011-4000-8000-000000000002"));
            migrationBuilder.DeleteData(table: "ProductSizeOptions", keyColumn: "Id", keyValue: new Guid("b1b2c3d4-0012-4000-8000-000000000002"));
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiGacKgId);
            migrationBuilder.DeleteData(table: "Products", keyColumn: "Id", keyValue: XoiXeoKgId);

            migrationBuilder.DropTable(
                name: "DailyInventories");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Products");
        }
    }
}
