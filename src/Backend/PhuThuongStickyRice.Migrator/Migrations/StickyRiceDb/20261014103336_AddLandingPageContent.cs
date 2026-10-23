using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class AddLandingPageContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LandingPageContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    HeroTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HeroTagline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HeroButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StoryTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StoryBody = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MenuTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MenuLabel1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MenuLabel2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MenuLabel3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewsTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewsItemsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FooterTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    FooterButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LandingPageContent", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "LandingPageContent",
                columns: new[]
                {
                    "Id", "HeroTitle", "HeroTagline", "HeroButtonText",
                    "StoryTitle", "StoryBody",
                    "MenuTitle", "MenuLabel1", "MenuLabel2", "MenuLabel3",
                    "NewsTitle", "NewsItemsJson",
                    "FooterTitle", "ContactAddress", "ContactPhone", "ContactEmail", "FooterButtonText",
                    "CreatedDateTime"
                },
                values: new object[]
                {
                    new Guid("b1c2d3e4-0001-4000-8000-000000000001"),
                    "XÔI PHÚ THƯỢNG",
                    "hương vị làng nghề, dẻo thơm mỗi sáng",
                    "Đặt món ngay",
                    "Câu chuyện làng nghề",
                    "Làng Phú Thượng, bên dòng sông Hồng của Hà Nội, đã gắn liền với nghề nấu xôi suốt hàng trăm năm. Mỗi sáng tinh mơ, tiếng chõ xôi bốc khói, mùi nếp cái hoa vàng quyện cùng đậu xanh, gấc chín đỏ au đã đánh thức cả xóm làng. Người Phú Thượng giữ nghề bằng đôi bàn tay khéo léo: hạt nếp phải chọn loại ngon nhất, đồ xôi đúng lửa, đúng thời gian để hạt xôi dẻo mà không nát. Hôm nay, chúng tôi mang trọn hương vị ấy đến bàn ăn của bạn — gói xôi nóng hổi, thơm dẻo như chính buổi sáng làng nghề.",
                    "Món ngon mỗi sáng",
                    "Xôi gấc", "Xôi xéo", "Xôi ngũ sắc",
                    "Chuyện nhà xôi",
                    "[{\"Title\":\"Món ăn sáng được yêu thích nhất tuần\",\"Body\":\"Xôi gấc của quán lọt top món ăn sáng được đặt nhiều nhất khu vực Tây Hồ trên ứng dụng đặt món, với hàng trăm đánh giá 5 sao từ thực khách.\",\"Source\":\"ShopeeFood\"},{\"Title\":\"Giữ trọn nghề nấu xôi trăm năm\",\"Body\":\"Từ đời bà, đời mẹ đến nay, gia đình vẫn giữ đúng công thức làng nghề Phú Thượng: nếp ngon, đồ hai lần lửa, hành phi tự làm mỗi sáng.\",\"Source\":\"Chủ quán chia sẻ\"},{\"Title\":\"Khách quen hơn 5 năm vẫn quay lại\",\"Body\":\"\\\"Xôi dẻo, đậu xanh bùi, hành phi giòn — ăn sáng ở đây thành thói quen của cả nhà tôi rồi. Gói nào cũng nóng hổi, đầy đặn.\\\"\",\"Source\":\"Chị Lan Anh, khách hàng\"}]",
                    "Ghé thăm quán",
                    "Số 2A,ngách 3 Ngõ 277 Xuân Đỉnh, Từ Liêm, Hà Nội",
                    "0389 245 468",
                    "admin@xoihuyen.vn",
                    "Đặt món ngay",
                    new DateTimeOffset(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), TimeSpan.Zero)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LandingPageContent",
                keyColumn: "Id",
                keyValue: new Guid("b1c2d3e4-0001-4000-8000-000000000001"));

            migrationBuilder.DropTable(
                name: "LandingPageContent");
        }
    }
}
