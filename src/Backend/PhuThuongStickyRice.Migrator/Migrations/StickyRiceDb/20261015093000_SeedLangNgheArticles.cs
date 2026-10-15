using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Item 94: seed 3 SEO articles about the Phú Thượng sticky-rice craft
    /// village (titles/descriptions target "xôi Phú Thượng" and
    /// "xôi ngon Hà Nội").
    /// </summary>
    public partial class SeedLangNgheArticles : Migration
    {
        private static readonly Guid Article1 = new Guid("f1a2b3c4-0001-4000-8000-000000000001");
        private static readonly Guid Article2 = new Guid("f1a2b3c4-0002-4000-8000-000000000002");
        private static readonly Guid Article3 = new Guid("f1a2b3c4-0003-4000-8000-000000000003");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seedDate = new DateTime(2026, 10, 15, 2, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[] { "Id", "Title", "Body", "ImageUrl", "PublishedAt", "IsPublished", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        Article1,
                        "Lịch sử xôi Phú Thượng — làng nghề trăm năm giữa lòng Hà Nội",
                        "Phú Thượng (Tây Hồ, Hà Nội) là làng nghề nấu xôi nổi tiếng hàng trăm năm. Từ thời ông bà, người dân làng đã dậy từ 3 giờ sáng để ngâm nếp, đồ xôi hai lần lửa, rồi gánh xôi đi bán khắp phố cổ. Ngày nay, xôi Phú Thượng vẫn giữ đúng công thức xưa: nếp cái hoa vàng, đậu xanh bùi, hành phi tự làm — đó là lý do nhiều người gọi đây là xôi ngon Hà Nội đáng thử nhất vào mỗi buổi sáng.",
                        null, seedDate, true, new DateTimeOffset(seedDate)
                    },
                    {
                        Article2,
                        "Quy trình đồ xôi ngon chuẩn làng nghề Phú Thượng",
                        "Bí quyết của xôi Phú Thượng nằm ở quy trình đồ hai lần lửa: lần một đồ chín tới rồi xới tơi, để nguội bớt; lần hai đồ lại cho hạt nếp dẻo mềm, bóng mẩy mà không nát. Nếp phải ngâm đủ 6-8 tiếng, nước đồ xôi giữ sôi đều, và hành phi phải tự phi mỗi sáng mới giòn thơm. Quán chúng tôi vẫn làm đúng từng bước này mỗi ngày — đó là cách xôi ngon Hà Nội ra đời.",
                        null, seedDate, true, new DateTimeOffset(seedDate)
                    },
                    {
                        Article3,
                        "Cách chọn nếp ngon để đồ xôi dẻo thơm như xôi Phú Thượng",
                        "Muốn đồ xôi ngon như xôi Phú Thượng, khâu chọn nếp quyết định một nửa: chọn nếp cái hoa vàng hạt mẩy, đều, màu trắng đục tự nhiên, không lẫn tấm. Nếp mới thơm hơn nếp để lâu; khi nhai thử hạt sống phải có vị ngọt nhẹ, không chua. Ngâm 6-8 tiếng với chút muối, vớt ráo rồi mới đồ — hạt nếp sẽ dẻo, ráo và bóng đẹp như xôi ngon Hà Nội chính hiệu.",
                        null, seedDate, true, new DateTimeOffset(seedDate)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "Articles", keyColumn: "Id", keyValue: Article1);
            migrationBuilder.DeleteData(table: "Articles", keyColumn: "Id", keyValue: Article2);
            migrationBuilder.DeleteData(table: "Articles", keyColumn: "Id", keyValue: Article3);
        }
    }
}
