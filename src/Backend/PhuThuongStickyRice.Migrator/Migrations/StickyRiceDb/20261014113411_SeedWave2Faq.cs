using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhuThuongStickyRice.Migrator.Migrations.StickyRiceDb
{
    /// <summary>
    /// Seeds the Wave 2 FAQ with common questions, and backfills the new
    /// ShopSetting columns on rows created before Wave 2.
    /// </summary>
    public partial class SeedWave2Faq : Migration
    {
        private static readonly (Guid Id, string Question, string Answer)[] Faqs =
        {
            (
                new Guid("e1b2c3d4-0001-4000-8000-000000000001"),
                "Shop giao hàng trong bao lâu?",
                "Nội thành Hà Nội shop giao trong khoảng 30–45 phút sau khi xác nhận đơn. " +
                "Bạn có thể xem thời gian dự kiến ngay tại trang đặt hàng."
            ),
            (
                new Guid("e1b2c3d4-0002-4000-8000-000000000002"),
                "Phí giao hàng tính thế nào?",
                "Phí giao hàng 20.000đ cho đơn dưới 200.000đ. Đơn từ 200.000đ trở lên " +
                "được miễn phí ship. Khách tự đến lấy tại quán luôn được miễn phí ship."
            ),
            (
                new Guid("e1b2c3d4-0003-4000-8000-000000000003"),
                "Shop có những hình thức thanh toán nào?",
                "Bạn có thể thanh toán khi nhận hàng (COD), chuyển khoản ngân hàng qua QR, " +
                "hoặc ví MoMo. Với đơn chuyển khoản từ 500.000đ, shop sẽ liên hệ để nhận đặt cọc trước."
            ),
            (
                new Guid("e1b2c3d4-0004-4000-8000-000000000004"),
                "Làm sao để tra cứu / hủy / sửa đơn hàng?",
                "Vào trang \"Tra cứu đơn hàng\", nhập mã đơn và số điện thoại đặt hàng. " +
                "Đơn đang ở trạng thái chờ xác nhận có thể hủy hoặc sửa số lượng trực tiếp."
            ),
            (
                new Guid("e1b2c3d4-0005-4000-8000-000000000005"),
                "Điểm tích lũy dùng thế nào?",
                "Mỗi 10.000đ chi tiêu trên đơn hoàn thành được 1 điểm. " +
                "100 điểm đổi được 10.000đ trừ vào đơn tiếp theo. " +
                "Khách chi tiêu tích lũy từ 1 triệu (hạng Bạc) và 5 triệu (hạng Vàng) " +
                "được giảm giá tự động 2% và 5%."
            ),
            (
                new Guid("e1b2c3d4-0006-4000-8000-000000000006"),
                "Shop có xuất hóa đơn VAT không?",
                "Có. Khi đặt hàng, bạn mở mục \"Cần hóa đơn VAT\" và nhập mã số thuế + tên công ty. " +
                "Shop sẽ xuất hóa đơn và gửi cho bạn sau khi đơn hoàn thành."
            ),
            (
                new Guid("e1b2c3d4-0007-4000-8000-000000000007"),
                "Đặt xôi định kỳ mỗi ngày thế nào?",
                "Vào trang \"Đặt xôi định kỳ\", chọn món, số lượng và ngày bắt đầu. " +
                "Mỗi sáng shop tự tạo đơn giao đến địa chỉ của bạn, thanh toán khi nhận hàng (COD). " +
                "Bạn có thể nhờ shop tạm dừng bất cứ lúc nào."
            ),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill Wave 2 ShopSetting defaults on rows created before Wave 2
            // (the columns were added with 0; the app also falls back on read).
            migrationBuilder.Sql(@"
UPDATE ShopSettings SET
    BankTransferExpiryMinutes = 30,
    PointsPerVnd = 10000,
    PointsRedeemUnit = 100,
    PointsRedeemValue = 10000,
    ReferralBonusPoints = 50,
    DepositThreshold = 500000,
    DepositPercent = 30,
    GiftFee = 20000,
    TierSilverMin = 1000000,
    TierSilverDiscount = 2,
    TierGoldMin = 5000000,
    TierGoldDiscount = 5
WHERE BankTransferExpiryMinutes = 0");

            for (var i = 0; i < Faqs.Length; i++)
            {
                migrationBuilder.InsertData(
                    table: "FaqItems",
                    columns: new[] { "Id", "Question", "Answer", "SortOrder", "IsActive" },
                    values: new object[] { Faqs[i].Id, Faqs[i].Question, Faqs[i].Answer, i + 1, true });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, _, _) in Faqs)
            {
                migrationBuilder.DeleteData(
                    table: "FaqItems",
                    keyColumn: "Id",
                    keyValue: id);
            }
        }
    }
}
