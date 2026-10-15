using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Items 164 + 165 + 166: daily sale time windows (AvailableHoursJson),
    /// daily portion limit (DailyLimit) and the photo gallery (ImageUrlsJson)
    /// on Products.
    /// </summary>
    public partial class AddProductHoursLimitImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailableHoursJson",
                table: "Products",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyLimit",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrlsJson",
                table: "Products",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableHoursJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DailyLimit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageUrlsJson",
                table: "Products");
        }
    }
}
