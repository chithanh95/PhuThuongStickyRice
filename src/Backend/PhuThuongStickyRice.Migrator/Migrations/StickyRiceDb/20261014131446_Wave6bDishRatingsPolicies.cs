using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <inheritdoc />
    public partial class Wave6bDishRatingsPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptingOrders",
                table: "ShopSettings",
                type: "bit",
                nullable: false,
                // Existing rows keep accepting orders after the upgrade.
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Hotline",
                table: "ShopSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZaloPhone",
                table: "ShopSettings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderReviewLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    OrderReviewId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReviewLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderReviewLines_OrderReviews_OrderReviewId",
                        column: x => x.OrderReviewId,
                        principalTable: "OrderReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyPages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderReviewLines_OrderReviewId",
                table: "OrderReviewLines",
                column: "OrderReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderReviewLines_ProductId",
                table: "OrderReviewLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyPages_Slug",
                table: "PolicyPages",
                column: "Slug",
                unique: true);

            // Wave 6b (item 28): seed the three policy pages.
            migrationBuilder.InsertData(
                table: "PolicyPages",
                columns: new[] { "Id", "Slug", "Title", "Body", "SortOrder", "CreatedDateTime" },
                values: new object[,]
                {
                    {
                        new Guid("6b6b6b6b-0001-4000-8000-000000000001"),
                        "giao-hang",
                        "Chính sách giao hàng",
                        "1. Khu vực giao hàng\nQuán giao xôi nóng trong nội thành và các khu vực lân cận. " +
                        "Khi đặt hàng, quý khách vui lòng ghi địa chỉ chi tiết (số nhà, ngõ/ngách, phường) " +
                        "và số điện thoại chính xác để shipper liên hệ.\n\n" +
                        "2. Phí giao hàng\nPhí giao hàng 20.000₫/đơn. Đơn có giá trị (tạm tính sau giảm giá) " +
                        "từ 200.000₫ trở lên được miễn phí giao hàng. Mức phí và ngưỡng miễn phí có thể được " +
                        "điều chỉnh và luôn hiển thị rõ ở trang thanh toán trước khi quý khách xác nhận.\n\n" +
                        "3. Thời gian giao hàng\nQuán chỉ nhận và giao đơn trong giờ mở cửa hiển thị trên website. " +
                        "Thời gian giao dự kiến 30–45 phút kể từ khi quán xác nhận đơn. Vào giờ cao điểm, " +
                        "shipper có thể đến trễ hơn — quán sẽ chủ động gọi báo cho quý khách.\n\n" +
                        "4. Kiểm tra khi nhận hàng\nQuý khách vui lòng kiểm tra món ăn ngay khi nhận. Nếu thiếu món, " +
                        "sai món hoặc món bị hư hỏng do vận chuyển, hãy báo ngay cho quán trong vòng 30 phút " +
                        "kèm ảnh chụp để được xử lý theo Chính sách đổi trả.",
                        1,
                        new DateTimeOffset(new DateTime(2026, 10, 14), TimeSpan.Zero)
                    },
                    {
                        new Guid("6b6b6b6b-0002-4000-8000-000000000002"),
                        "doi-tra",
                        "Chính sách đổi trả",
                        "1. Trường hợp được đổi/trả\nQuán đổi món mới hoặc hoàn tiền khi: giao sai món, thiếu món, " +
                        "món bị hư hỏng, có vật lạ, hoặc chất lượng không đảm bảo do lỗi của quán.\n\n" +
                        "2. Cách thức yêu cầu\nQuý khách liên hệ hotline/Zalo của quán trong vòng 30 phút kể từ khi " +
                        "nhận hàng, kèm ảnh chụp món ăn và mã đơn hàng. Quán xác nhận và xử lý trong ngày.\n\n" +
                        "3. Hình thức xử lý\nTùy trường hợp, quán sẽ giao bù món, đổi món khác tương đương, hoặc hoàn " +
                        "tiền (tiền mặt khi shipper quay lại, hoặc chuyển khoản trong 1–2 ngày làm việc).\n\n" +
                        "4. Trường hợp không áp dụng\nQuán không đổi/trả khi khách đổi ý sau khi món đã được chế biến, " +
                        "khi món hư hỏng do để quá lâu sau khi nhận, hoặc khi không cung cấp được ảnh/mã đơn để " +
                        "xác minh. Đơn đặt ảo, spam sẽ bị từ chối phục vụ.",
                        2,
                        new DateTimeOffset(new DateTime(2026, 10, 14), TimeSpan.Zero)
                    },
                    {
                        new Guid("6b6b6b6b-0003-4000-8000-000000000003"),
                        "dieu-khoan",
                        "Điều khoản sử dụng",
                        "1. Đặt hàng\nKhi đặt hàng trên website, quý khách cần cung cấp tên, số điện thoại và địa " +
                        "chỉ chính xác. Số điện thoại là căn cứ để quán xác nhận đơn, tra cứu và xử lý khiếu nại — " +
                        "quý khách chịu trách nhiệm về tính chính xác của thông tin đã cung cấp.\n\n" +
                        "2. Giá và thanh toán\nGiá niêm yết trên website là giá cuối cùng quý khách thanh toán, đã bao " +
                        "gồm phí chế biến. Phí giao hàng (nếu có) hiển thị riêng ở trang thanh toán. Quán nhận thanh " +
                        "toán tiền mặt khi nhận hàng, chuyển khoản ngân hàng và ví MoMo.\n\n" +
                        "3. Xác nhận và hủy đơn\nĐơn hàng chỉ có hiệu lực sau khi quán xác nhận (gọi điện/Zalo hoặc cập " +
                        "nhật trạng thái trên website). Quý khách có thể tự hủy đơn đang ở trạng thái \"Chờ xác nhận\" " +
                        "tại trang tra cứu đơn hàng. Quán có quyền hủy đơn khi không liên lạc được với khách, khi " +
                        "hết nguyên liệu, hoặc khi phát hiện gian lận.\n\n" +
                        "4. Đánh giá\nCác đánh giá trên website phải trung thực, dựa trên trải nghiệm thực tế. Quán " +
                        "có quyền ẩn các đánh giá chứa nội dung xúc phạm, quảng cáo hoặc không liên quan.\n\n" +
                        "5. Liên hệ\nMọi thắc mắc về đơn hàng và các chính sách, quý khách liên hệ hotline/Zalo của " +
                        "quán trong giờ mở cửa để được hỗ trợ nhanh nhất.",
                        3,
                        new DateTimeOffset(new DateTime(2026, 10, 14), TimeSpan.Zero)
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PolicyPages",
                keyColumn: "Slug",
                keyValues: new object[] { "giao-hang", "doi-tra", "dieu-khoan" });

            migrationBuilder.DropTable(
                name: "OrderReviewLines");

            migrationBuilder.DropTable(
                name: "PolicyPages");

            migrationBuilder.DropColumn(
                name: "AcceptingOrders",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "Hotline",
                table: "ShopSettings");

            migrationBuilder.DropColumn(
                name: "ZaloPhone",
                table: "ShopSettings");
        }
    }
}
