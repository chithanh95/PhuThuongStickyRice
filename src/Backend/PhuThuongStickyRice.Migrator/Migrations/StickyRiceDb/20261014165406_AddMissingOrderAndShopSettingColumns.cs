using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class AddMissingOrderAndShopSettingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns exist on the domain entities (and in the model
            // snapshot) but the wave migrations that introduced them never
            // emitted the AddColumn calls, so databases migrated earlier are
            // missing them ("Invalid column name" at runtime).
            migrationBuilder.AddColumn<bool>(
                name: "MoMoMatched",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RefundStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CheckInPoints",
                table: "ShopSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyTargetAmount",
                table: "ShopSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NepPerPortionGrams",
                table: "ShopSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MoMoMatched", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundStatus", table: "Orders");
            migrationBuilder.DropColumn(name: "CheckInPoints", table: "ShopSettings");
            migrationBuilder.DropColumn(name: "DailyTargetAmount", table: "ShopSettings");
            migrationBuilder.DropColumn(name: "NepPerPortionGrams", table: "ShopSettings");
        }
    }
}
