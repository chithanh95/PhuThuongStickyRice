using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class AddLandingConversionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookPixelId",
                table: "ShopSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleAnalyticsId",
                table: "ShopSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PromoCountdownActive",
                table: "LandingPageContent",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PromoCountdownEnd",
                table: "LandingPageContent",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoCountdownText",
                table: "LandingPageContent",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromoSubscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "landing"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoSubscribers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromoSubscribers_Phone",
                table: "PromoSubscribers",
                column: "Phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoSubscribers");

            migrationBuilder.DropColumn(
                name: "FacebookPixelId",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "GoogleAnalyticsId",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "PromoCountdownActive",
                table: "LandingPageContent");

            migrationBuilder.DropColumn(
                name: "PromoCountdownEnd",
                table: "LandingPageContent");

            migrationBuilder.DropColumn(
                name: "PromoCountdownText",
                table: "LandingPageContent");
        }
    }
}
