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
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Reports;

namespace PhuThuongStickyRice.Application.Reports
{
    /// <summary>
    /// Item 85: weekly (Monday 7:00) + monthly (1st, 7:00) owner reports.
    /// The summary is queued through the existing EmailMessage pipeline to
    /// <c>ShopSetting.OwnerReportEmail</c> (empty = disabled).
    /// </summary>
    public interface IOwnerReportAppService : IApplicationService
    {
        /// <summary>period = "weekly" or "monthly".</summary>
        Task<OwnerReportResultDto> SendAsync(string period);
    }

    [Authorize(Roles = "Admin")]
    public class OwnerReportAppService : ApplicationService, IOwnerReportAppService
    {
        private readonly IShopSettingAppService _shopSettings;
        private readonly IDashboardAppService _dashboard;
        private readonly IEmailMessageRepository _emailMessages;

        public OwnerReportAppService(
            IShopSettingAppService shopSettings,
            IDashboardAppService dashboard,
            IEmailMessageRepository emailMessages)
        {
            _shopSettings = shopSettings;
            _dashboard = dashboard;
            _emailMessages = emailMessages;
        }

        /// <summary>Internal — the background worker calls this.</summary>
        [AllowAnonymous]
        public virtual async Task<OwnerReportResultDto> SendAsync(string period)
        {
            var settings = await _shopSettings.GetAsync();
            var to = (settings.OwnerReportEmail ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(to))
            {
                return new OwnerReportResultDto
                {
                    Period = period,
                    Sent = false,
                    Reason = "Chưa cấu hình email nhận báo cáo (OwnerReportEmail)."
                };
            }

            // Vietnam time range: weekly = last Mon 00:00 -> this Mon 00:00,
            // monthly = last month.
            var vnNow = DateTime.UtcNow.AddHours(7);
            DateTime fromVn, toVn;
            string periodLabel;
            if (string.Equals(period, "monthly", StringComparison.OrdinalIgnoreCase))
            {
                var firstOfThisMonth = new DateTime(vnNow.Year, vnNow.Month, 1);
                fromVn = firstOfThisMonth.AddMonths(-1);
                toVn = firstOfThisMonth;
                periodLabel = $"tháng {fromVn:MM/yyyy}";
            }
            else
            {
                period = "weekly";
                var daysSinceMonday = ((int)vnNow.DayOfWeek + 6) % 7;
                toVn = vnNow.Date.AddDays(-daysSinceMonday);
                fromVn = toVn.AddDays(-7);
                periodLabel = $"tuần {fromVn:dd/MM} – {toVn.AddDays(-1):dd/MM/yyyy}";
            }

            var fromUtc = fromVn.AddHours(-7);
            var toUtc = toVn.AddHours(-7);

            var stats = await _dashboard.GetStatsAsync(fromUtc, toUtc);
            var topProducts = await _dashboard.GetTopProductsAsync(fromUtc, toUtc, 5);

            var body = ComposeBody(periodLabel, stats, topProducts);
            await _emailMessages.AddAsync(new EmailMessage
            {
                From = "no-reply@phuthuong.vn",
                Tos = to,
                Subject = $"[Xôi Phú Thượng] Báo cáo {periodLabel}",
                Body = body
            });
            await _emailMessages.UnitOfWork.SaveChangesAsync();

            return new OwnerReportResultDto { Period = period, Sent = true };
        }

        private static string ComposeBody(
            string periodLabel,
            Orders.DTOs.DashboardStatsDto stats,
            System.Collections.Generic.List<Orders.DTOs.TopProductDto> topProducts)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"BÁO CÁO DOANH THU {periodLabel.ToUpperInvariant()} — XÔI PHÚ THƯỢNG");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine($"Tổng đơn hàng: {stats.TotalOrders}");
            sb.AppendLine($"Doanh thu: {stats.TotalRevenue:N0} ₫ (không tính đơn đã hủy)");
            sb.AppendLine($"Món đã bán: {stats.TotalProductsSold}");
            sb.AppendLine($"Đơn chờ xác nhận (hiện tại): {stats.PendingOrders}");
            sb.AppendLine();
            sb.AppendLine("Đơn hàng theo trạng thái:");
            foreach (var s in stats.OrdersByStatus ?? Enumerable.Empty<Orders.DTOs.OrdersByStatusDto>())
            {
                sb.AppendLine($"  - {s.Status}: {s.Count}");
            }

            sb.AppendLine();
            sb.AppendLine("Top 5 món bán chạy:");
            var rank = 0;
            foreach (var p in topProducts ?? Enumerable.Empty<Orders.DTOs.TopProductDto>())
            {
                rank++;
                sb.AppendLine($"  {rank}. {p.ProductName}: {p.QuantitySold} gói — {p.Revenue:N0} ₫");
            }

            sb.AppendLine();
            sb.AppendLine("-- Báo cáo tự động từ hệ thống Xôi Phú Thượng --");
            return sb.ToString();
        }
    }
}
