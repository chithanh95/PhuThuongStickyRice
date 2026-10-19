using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhuThuongStickyRice.Persistence;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    [DbContext(typeof(StickyRiceDbContext))]
    [Migration("20261019191500_AddProductPromoPrice")]
    public partial class AddProductPromoPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Item 256: giá khuyến mãi của món. Nullable — null = không có KM.
            // Card món hiện "giá gốc → giá KM" khi PromoPrice < Price.
            migrationBuilder.AddColumn<decimal>(
                name: "PromoPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromoPrice",
                table: "Products");
        }
    }
}
