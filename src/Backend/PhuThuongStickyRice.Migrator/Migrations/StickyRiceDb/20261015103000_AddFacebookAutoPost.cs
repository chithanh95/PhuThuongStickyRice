using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Item 130: auto-post new products to the Facebook Page feed.
    /// Adds ShopSettings.FacebookPageId (reuses the existing encrypted
    /// FacebookPageAccessToken) and the FacebookPostLogs history table.
    /// </summary>
    public partial class AddFacebookAutoPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookPageId",
                table: "ShopSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacebookPostLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PostUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacebookPostLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacebookPostLogs_CreatedAt",
                table: "FacebookPostLogs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacebookPostLogs");

            migrationBuilder.DropColumn(
                name: "FacebookPageId",
                table: "ShopSettings");
        }
    }
}
