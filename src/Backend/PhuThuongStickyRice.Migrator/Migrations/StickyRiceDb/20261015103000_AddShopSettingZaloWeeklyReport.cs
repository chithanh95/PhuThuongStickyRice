using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Item 138: weekly owner report via Zalo OA — Zalo OA access token
    /// (data-protected at rest), owner phone, owner Zalo user id, and the
    /// Monday-worker idempotency marker on ShopSettings.
    /// </summary>
    public partial class AddShopSettingZaloWeeklyReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZaloOaAccessToken",
                table: "ShopSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerPhone",
                table: "ShopSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerZaloUserId",
                table: "ShopSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastWeeklyReportSentWeek",
                table: "ShopSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZaloOaAccessToken",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "OwnerPhone",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "OwnerZaloUserId",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "LastWeeklyReportSentWeek",
                table: "ShopSettings");
        }
    }
}
