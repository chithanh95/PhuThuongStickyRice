using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Shop;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Reports;

namespace PhuThuongStickyRice.Application.Reports
{
    /// <summary>
    /// Item 85: weekly (Monday 7:00) + monthly (1st, 7:00) owner reports.
    /// The summary is queued through the existing EmailMessage pipeline recipientEmail
    /// <c>ShopSetting.OwnerReportEmail</c> (empty = disabled).
    /// </summary>
    public interface IOwnerReportAppService : IApplicationService
    {
        /// <summary>period = "weekly" or "monthly".</summary>
        Task<OwnerReportResultDto> SendAsync(string period);
    }

    /// <summary>
    /// Báo cáo định kỳ cho chủ quán (item 85): báo cáo tuần (thứ 2, 7:00) và
    /// báo cáo tháng (ngày 1, 7:00) do background worker gọi. Tóm tắt doanh thu,
    /// đơn hàng theo trạng thái và top 5 món bán chạy; nội dung được xếp hàng
    /// qua pipeline EmailMessage gửi đến địa chỉ OwnerReportEmail trong cài đặt
    /// shop (bỏ trống = tắt báo cáo).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class OwnerReportAppService : ApplicationService, IOwnerReportAppService
    {
        private readonly IShopSettingAppService _shopSettings;
        private readonly IDashboardAppService _dashboard;
        private readonly IEmailMessageRepository _emailMessages;

        /// <summary>
        /// Khởi tạo service báo cáo cho chủ quán (Admin): tổng hợp dashboard,
        /// cài đặt shop và tin nhắn email thành báo cáo tổng quan.
        /// </summary>
        public OwnerReportAppService(
            IShopSettingAppService shopSettings,
            IDashboardAppService dashboard,
            IEmailMessageRepository emailMessages)
        {
            _shopSettings = shopSettings;
            _dashboard = dashboard;
            _emailMessages = emailMessages;
        }

        // Báo cáo tuần lấy 7 ngày (thứ 2 tuần trước 00:00 → thứ 2 tuần này 00:00).
        private const int WeeklyReportDays = 7;
        // Số món bán chạy nhất đưa vào báo cáo dùng chung: ReportLabels.TopProductsCount.
        // Địa chỉ gửi của báo cáo qua pipeline EmailMessage — Gmail của
        // quán, phải trùng tài khoản SMTP đã cấu hình (như FromAddress của
        // mail xác nhận đơn).
        private const string ReportSenderAddress = "Maihuyen280579@gmail.com";
        // Độ dài đường kẻ "=" phân cách tiêu đề báo cáo text gửi chủ quán.
        private const int SeparatorLength = 50;

        /// <summary>
        /// Background worker gọi để gửi báo cáo cho chủ quán. Nếu chưa cấu
        /// hình OwnerReportEmail trong cài đặt shop thì không gửi (trả về
        /// IsSent=false với lý do) thay vì ném lỗi — worker vẫn chạy ổn định.
        /// </summary>
        /// <param name="period">"weekly" (tuần trước) hoặc "monthly" (tháng trước).</param>
        /// <returns>Kết quả gửi: đã gửi hay bỏ qua + lý do.</returns>
        [AllowAnonymous]
        public virtual async Task<OwnerReportResultDto> SendAsync(string period)
        {
            var settings = await _shopSettings.GetAsync();
            var recipientEmail = (settings.OwnerReportEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return new OwnerReportResultDto
                {
                    Period = period,
                    IsSent = false,
                    Reason = "Chưa cấu hình email nhận báo cáo (OwnerReportEmail)."
                };
            }

            var (fromVn, toVn, periodLabel, normalizedPeriod) = ResolvePeriodRange(period);
            var fromUtc = fromVn.AddHours(-VietnamTime.UtcOffsetHours);
            var toUtc = toVn.AddHours(-VietnamTime.UtcOffsetHours);

            var stats = await _dashboard.GetStatsAsync(fromUtc, toUtc);
            var topProducts = await _dashboard.GetTopProductsAsync(fromUtc, toUtc, ReportLabels.TopProductsCount);

            var body = ComposeBody(periodLabel, stats, topProducts);
            await _emailMessages.AddAsync(new EmailMessage
            {
                From = ReportSenderAddress,
                Tos = recipientEmail,
                Subject = $"[Xôi Huyền] Báo cáo {periodLabel}",
                Body = body
            });
            await _emailMessages.UnitOfWork.SaveChangesAsync();

            return new OwnerReportResultDto { Period = normalizedPeriod, IsSent = true };
        }

        /// <summary>
        /// Chuẩn hóa kỳ báo cáo theo giờ Việt Nam: "monthly" = tháng trước,
        /// còn lại = tuần trước (thứ 2 00:00 → thứ 2 00:00).
        /// </summary>
        private static (DateTime FromVn, DateTime ToVn, string PeriodLabel, string NormalizedPeriod)
            ResolvePeriodRange(string period)
        {
            var vnNow = DateTime.UtcNow.AddHours(VietnamTime.UtcOffsetHours);
            if (string.Equals(period, "monthly", StringComparison.OrdinalIgnoreCase))
            {
                var firstOfThisMonth = new DateTime(vnNow.Year, vnNow.Month, 1);
                var monthFromVn = firstOfThisMonth.AddMonths(-1);
                var monthToVn = firstOfThisMonth;
                return (monthFromVn, monthToVn, $"tháng {monthFromVn:MM/yyyy}", "monthly");
            }

            var daysSinceMonday = ((int)vnNow.DayOfWeek + 6) % 7;
            var weekToVn = vnNow.Date.AddDays(-daysSinceMonday);
            var weekFromVn = weekToVn.AddDays(-WeeklyReportDays);
            return (weekFromVn, weekToVn,
                $"tuần {weekFromVn:dd/MM} – {weekToVn.AddDays(-1):dd/MM/yyyy}", "weekly");
        }

        /// <summary>
        /// Dựng nội dung email báo cáo cho chủ quán: tiêu đề kỳ, tổng quan
        /// doanh thu (đã loại đơn hủy), trạng thái đơn và top món bán chạy.
        /// Số lượng top món dùng chung hằng <c>TopProductsCount</c> với câu
        /// query ở <c>SendAsync</c> để báo cáo không lệch dữ liệu lấy về.
        /// </summary>
        /// <param name="periodLabel">Nhãn kỳ báo cáo (vd "tháng 09/2026").</param>
        /// <param name="stats">Thống kê dashboard của kỳ.</param>
        /// <param name="topProducts">Top món bán chạy đã query sẵn trong kỳ.</param>
        /// <returns>Nội dung email dạng văn bản thuần.</returns>
        private static string ComposeBody(
            string periodLabel,
            Orders.DTOs.DashboardStatsDto stats,
            System.Collections.Generic.List<Orders.DTOs.TopProductDto> topProducts)
        {
            var sb = new StringBuilder();
            sb.AppendLine(VietnameseCulture.Value, $"BÁO CÁO DOANH THU {periodLabel.ToUpperInvariant()} — XÔI HUYỀN");
            sb.AppendLine(new string('=', SeparatorLength));
            sb.AppendLine(VietnameseCulture.Value, $"Tổng đơn hàng: {stats.TotalOrders}");
            sb.AppendLine(VietnameseCulture.Value, $"Doanh thu: {stats.TotalRevenue:N0} ₫ (không tính đơn đã hủy)");
            sb.AppendLine(VietnameseCulture.Value, $"Món đã bán: {stats.TotalProductsSold}");
            sb.AppendLine(VietnameseCulture.Value, $"Đơn chờ xác nhận (hiện tại): {stats.PendingOrders}");
            sb.AppendLine();
            sb.AppendLine("Đơn hàng theo trạng thái:");
            foreach (var statusRow in stats.OrdersByStatus ?? Enumerable.Empty<Orders.DTOs.OrdersByStatusDto>())
            {
                sb.AppendLine(VietnameseCulture.Value, $"  - {statusRow.Status}: {statusRow.Count}");
            }

            sb.AppendLine();
            // [codequality-smell] Không hardcode số 5 — dùng chung hằng
            // TopProductsCount với câu query ở SendAsync.
            sb.AppendLine(VietnameseCulture.Value, $"Top {ReportLabels.TopProductsCount} món bán chạy:");
            var rank = 0;
            foreach (var product in topProducts ?? Enumerable.Empty<Orders.DTOs.TopProductDto>())
            {
                rank++;
                sb.AppendLine(VietnameseCulture.Value, $"  {rank}. {product.ProductName}: {product.QuantitySold} gói — {product.Revenue:N0} ₫");
            }

            sb.AppendLine();
            sb.AppendLine("-- Báo cáo tự động từ hệ thống Xôi Huyền --");
            return sb.ToString();
        }
    }
}
