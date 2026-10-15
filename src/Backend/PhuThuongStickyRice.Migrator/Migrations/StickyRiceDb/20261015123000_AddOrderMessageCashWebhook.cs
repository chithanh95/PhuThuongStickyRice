using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Items 162/163/180:
    /// - 162: OrderMessageTemplates table + 4 seeded templates
    ///   (Confirmed/Preparing/Completed/Cancelled).
    /// - 163: Orders.CashCollected / Orders.CashCollectedAt.
    /// - 180: ShopSettings.NewOrderWebhookUrl / NewOrderWebhookEnabled.
    /// </summary>
    public partial class AddOrderMessageCashWebhook : Migration
    {
        private static readonly Guid TemplateConfirmed = new Guid("a1a2a3a4-0001-4000-8000-000000000162");
        private static readonly Guid TemplatePreparing = new Guid("a1a2a3a4-0002-4000-8000-000000000162");
        private static readonly Guid TemplateCompleted = new Guid("a1a2a3a4-0003-4000-8000-000000000162");
        private static readonly Guid TemplateCancelled = new Guid("a1a2a3a4-0004-4000-8000-000000000162");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderMessageTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderMessageTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderMessageTemplates_Status",
                table: "OrderMessageTemplates",
                column: "Status",
                unique: true);

            // Item 163: cash-collection flag on orders.
            migrationBuilder.AddColumn<bool>(
                name: "CashCollected",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CashCollectedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            // Item 180: simple new-order webhook settings.
            migrationBuilder.AddColumn<string>(
                name: "NewOrderWebhookUrl",
                table: "ShopSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NewOrderWebhookEnabled",
                table: "ShopSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Item 162: seed 4 default templates (all non-nullable columns).
            var seedDate = new DateTime(2026, 10, 15, 3, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "OrderMessageTemplates",
                columns: new[] { "Id", "Status", "Template", "IsActive", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        TemplateConfirmed,
                        "Confirmed",
                        "Chào {TenKhach}! Quán Xôi Phú Thượng đã nhận đơn {MaDon} ({TongTien}). Quán đang chuẩn bị, sẽ báo lại khi xong ạ. Cảm ơn bạn đã ủng hộ!",
                        true,
                        new DateTimeOffset(seedDate)
                    },
                    {
                        TemplatePreparing,
                        "Preparing",
                        "Chào {TenKhach}! Đơn {MaDon} của bạn đang được đồ nóng hổi trên bếp. Quán sẽ báo ngay khi xong để bạn qua lấy / shipper đến nhận ạ!",
                        true,
                        new DateTimeOffset(seedDate)
                    },
                    {
                        TemplateCompleted,
                        "Completed",
                        "Chào {TenKhach}! Đơn {MaDon} đã hoàn thành. Chúc bạn ngon miệng với xôi Phú Thượng! Nếu hài lòng, bạn đánh giá giúp quán 5 sao nhé. Cảm ơn bạn!",
                        true,
                        new DateTimeOffset(seedDate)
                    },
                    {
                        TemplateCancelled,
                        "Cancelled",
                        "Chào {TenKhach}! Rất tiếc đơn {MaDon} đã bị hủy. Nếu bạn cần đặt lại hoặc có thắc mắc, nhắn lại cho quán nhé. Mong được phục vụ bạn lần sau!",
                        true,
                        new DateTimeOffset(seedDate)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "OrderMessageTemplates", keyColumn: "Id", keyValue: TemplateConfirmed);
            migrationBuilder.DeleteData(table: "OrderMessageTemplates", keyColumn: "Id", keyValue: TemplatePreparing);
            migrationBuilder.DeleteData(table: "OrderMessageTemplates", keyColumn: "Id", keyValue: TemplateCompleted);
            migrationBuilder.DeleteData(table: "OrderMessageTemplates", keyColumn: "Id", keyValue: TemplateCancelled);

            migrationBuilder.DropColumn(
                name: "CashCollected",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CashCollectedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NewOrderWebhookUrl",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "NewOrderWebhookEnabled",
                table: "ShopSettings");

            migrationBuilder.DropTable(
                name: "OrderMessageTemplates");
        }
    }
}
