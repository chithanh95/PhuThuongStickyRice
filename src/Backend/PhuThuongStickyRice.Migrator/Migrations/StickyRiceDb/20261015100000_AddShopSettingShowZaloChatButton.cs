using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Item 101: per-product "Hỏi qua Zalo" button toggle on /shop.
    /// Existing rows default to true (button visible).
    /// </summary>
    public partial class AddShopSettingShowZaloChatButton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowZaloChatButton",
                table: "ShopSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowZaloChatButton",
                table: "ShopSettings");
        }
    }
}
