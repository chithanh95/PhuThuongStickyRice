#nullable enable annotations
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Zalo;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Reports
{
    /// <summary>
    /// Item 138: builds and delivers the Monday-morning weekly owner
    /// report. Deliberately NOT an <c>ApplicationService</c> and carries
    /// no <c>[Authorize]</c> attributes so the BackgroundServer worker
    /// (unauthenticated) can call it; the HTTP surface
    /// (<see cref="IWeeklyReportAppService"/>) is Admin-only.
    /// </summary>
    public interface IWeeklyReportSender : ITransientDependency
    {
        /// <summary>Aggregates the last full Monday–Sunday week (Vietnam time).</summary>
        Task<WeeklyReportDto> BuildReportAsync();

        /// <summary>Renders the report as plain text for Zalo / copy-paste.</summary>
        string BuildMessageText(WeeklyReportDto report);

        /// <summary>
        /// Builds the report, delivers it via Zalo OA when configured, and
        /// records the idempotency marker. Never throws for a missing token —
        /// it returns <c>Sent = false</c> with the text to copy manually.
        /// </summary>
        Task<WeeklyReportSendResultDto> SendAsync();
    }

    public class WeeklyReportSender : IWeeklyReportSender
    {
        private const string EncryptedPrefix = "DPv1:";

        /// <summary>Số ngày trong một tuần — dùng khi tính khoảng tuần trước/sau.</summary>
        private const int DaysPerWeek = 7;

        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<ShopSetting, Guid> _settings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IZaloNotificationService _zalo;
        private readonly IDataProtector _protector;
        private readonly ILogger<WeeklyReportSender> _logger;

        public WeeklyReportSender(
            IRepository<Order, Guid> orders,
            IRepository<ShopSetting, Guid> settings,
            IAsyncQueryableExecuter asyncExecuter,
            IZaloNotificationService zalo,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<WeeklyReportSender> logger)
        {
            _orders = orders;
            _settings = settings;
            _asyncExecuter = asyncExecuter;
            _zalo = zalo;
            // Same purpose string as ShopSettingAppService so tokens saved
            // there can be decrypted here.
            _protector = dataProtectionProvider.CreateProtector("PhuThuongStickyRice.ShopSettings");
            _logger = logger;
        }

        /// <summary>
        /// The last full Monday–Sunday week (Vietnam dates) before
        /// <paramref name="vnToday"/>. On a Monday morning this is the week
        /// that just ended.
        /// </summary>
        public static (DateTime Start, DateTime End) LastFullWeek(DateTime vnToday)
        {
            var today = vnToday.Date;
            var daysSinceMonday = ((int)today.DayOfWeek + (DaysPerWeek - 1)) % DaysPerWeek;
            var start = today.AddDays(-daysSinceMonday - DaysPerWeek);
            return (start, start.AddDays(DaysPerWeek - 1));
        }

        /// <summary>Idempotency marker stored on ShopSetting.</summary>
        public static string WeekMarker(DateTime weekStart) =>
            weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public async Task<WeeklyReportDto> BuildReportAsync()
        {
            var (start, end) = LastFullWeek(VietnamTime.Today());
            var current = await AggregateWeekAsync(start, end);
            var (prevStart, prevEnd) = (start.AddDays(-DaysPerWeek), end.AddDays(-DaysPerWeek));
            var previous = await AggregateWeekAsync(prevStart, prevEnd);

            var weekNo = ISOWeek.GetWeekOfYear(start);
            var dto = new WeeklyReportDto
            {
                WeekStart = start,
                WeekEnd = end,
                WeekLabel = $"Tuần {weekNo} ({start:dd/MM} – {end:dd/MM/yyyy})",
                TotalRevenue = current.Revenue,
                OrderCount = current.OrderCount,
                CancelledCount = current.CancelledCount,
                AverageOrderValue = current.OrderCount > 0
                    ? current.Revenue / current.OrderCount
                    : 0m,
                TopProducts = current.TopProducts,
                PrevWeekRevenue = previous.Revenue,
                PrevWeekOrderCount = previous.OrderCount,
                RevenueChangePercent = ChangePercent(current.Revenue, previous.Revenue),
                OrderChangePercent = ChangePercent(current.OrderCount, previous.OrderCount),
                GeneratedAt = VietnamTime.Now.DateTime
            };
            dto.MessageText = BuildMessageText(dto);
            return dto;
        }

        public string BuildMessageText(WeeklyReportDto report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📊 BÁO CÁO TUẦN — XÔI PHÚ THƯỢNG");
            sb.AppendLine(report.WeekLabel);
            sb.AppendLine();
            sb.Append("💰 Doanh thu: ").Append(FormatVnd(report.TotalRevenue));
            sb.AppendLine(ChangeSuffix(report.RevenueChangePercent, "doanh thu"));
            sb.Append("🧾 Số đơn: ").Append(report.OrderCount.ToString("N0", CultureInfo.InvariantCulture));
            sb.AppendLine(ChangeSuffix(report.OrderChangePercent, "số đơn"));
            sb.AppendLine($"❌ Đơn hủy: {report.CancelledCount:N0}");
            sb.AppendLine($"🧮 Giá trị TB/đơn: {FormatVnd(report.AverageOrderValue)}");
            sb.AppendLine();
            if (report.TopProducts.Count > 0)
            {
                sb.AppendLine("🏆 Món bán chạy:");
                var i = 1;
                foreach (var p in report.TopProducts)
                {
                    sb.AppendLine($"{i}. {p.ProductName} — {p.Quantity:N0} suất");
                    i++;
                }
            }
            else
            {
                sb.AppendLine("🏆 Chưa có món nào được bán trong tuần.");
            }
            sb.AppendLine();
            sb.Append("— Tự động gửi 08:00 sáng thứ 2 hàng tuần —");
            return sb.ToString();
        }

        public async Task<WeeklyReportSendResultDto> SendAsync()
        {
            var report = await BuildReportAsync();
            var message = BuildMessageText(report);
            var marker = WeekMarker(report.WeekStart);

            var settings = await GetSingleSettingAsync();
            var token = Unprotect(settings?.ZaloOaAccessToken);
            var recipient = string.IsNullOrWhiteSpace(settings?.OwnerZaloUserId)
                ? settings?.OwnerPhone?.Trim()
                : settings.OwnerZaloUserId.Trim();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(recipient))
            {
                // Still mark the week so the Monday worker doesn't retry
                // every 10 minutes all morning.
                await MarkSentAsync(settings, marker);
                _logger.LogInformation(
                    "Weekly report {Week}: Zalo OA not configured — returning text for manual copy.",
                    marker);
                return new WeeklyReportSendResultDto
                {
                    IsSent = false,
                    ZaloConfigured = false,
                    MessageText = message,
                    Note = "Chưa cấu hình Zalo OA (thiếu Access Token hoặc Zalo User ID/SĐT chủ quán ở Cài đặt cửa hàng). " +
                           "Báo cáo ở bên dưới — bấm \"Sao chép nội dung\" để gửi tay, không giả vờ đã gửi."
                };
            }

            await _zalo.SendTextMessageAsync(recipient, message);
            await MarkSentAsync(settings, marker);
            _logger.LogInformation(
                "Weekly report {Week} handed to the Zalo OA service for {Recipient}.",
                marker, recipient);

            return new WeeklyReportSendResultDto
            {
                IsSent = true,
                ZaloConfigured = true,
                MessageText = message,
                Note = "Đã chuyển báo cáo tới dịch vụ Zalo OA. Lưu ý: tích hợp Zalo OA hiện ở chế độ " +
                       "stub (ghi log) — xem log server để kiểm tra nội dung; cần triển khai bước gửi " +
                       "thật qua Zalo OA API khi quán có token chính thức."
            };
        }

        /// <summary>
        /// Tên hiển thị khi dòng đơn không có tên sản phẩm (dữ liệu cũ bị thiếu).
        /// </summary>
        private const string UnnamedProductLabel = "(không tên)";

        /// <summary>
        /// Số sản phẩm bán chạy nhất hiển thị trong báo cáo tuần gửi chủ quán.
        /// </summary>
        private const int TopProductsCount = 5;

        /// <summary>
        /// Dòng đơn rút gọn cho báo cáo tuần: chỉ trạng thái, tổng tiền và các
        /// dòng hàng cần thiết — tránh materialize cả entity Order.
        /// </summary>
        private sealed record OrderWeekRow(
            OrderStatus Status,
            decimal Total,
            List<OrderWeekLine> Lines);

        /// <summary>Dòng hàng rút gọn: tên sản phẩm + số lượng đã bán.</summary>
        private sealed record OrderWeekLine(string ProductName, int Quantity);

        private async Task<WeekAggregate> AggregateWeekAsync(DateTime weekStartVn, DateTime weekEndVn)
        {
            var fromUtc = VietnamTime.DayStartUtc(weekStartVn);
            var toUtc = VietnamTime.DayEndExclusiveUtc(weekEndVn);

            var queryable = await _orders.GetQueryableAsync();
            var rows = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
                    .Select(x => new OrderWeekRow(
                        x.Status,
                        x.TotalAmount.Amount,
                        x.Lines.Select(l => new OrderWeekLine(l.ProductName, l.Quantity)).ToList())));

            var billable = rows.Where(x => x.Status != OrderStatus.Cancelled).ToList();
            var productQty = AccumulateProductQuantities(billable);

            return new WeekAggregate
            {
                Revenue = billable.Sum(x => x.Total),
                OrderCount = billable.Count,
                CancelledCount = rows.Count(x => x.Status == OrderStatus.Cancelled),
                TopProducts = productQty
                    .OrderByDescending(kv => kv.Value)
                    .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(TopProductsCount)
                    .Select(kv => new WeeklyTopProductDto
                    {
                        ProductName = kv.Key,
                        Quantity = kv.Value
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Cộng dồn số lượng đã bán theo tên sản phẩm (không phân biệt hoa/thường).
        /// </summary>
        private static Dictionary<string, long> AccumulateProductQuantities(List<OrderWeekRow> billable)
        {
            var productQty = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in billable)
            {
                foreach (var l in o.Lines)
                {
                    var name = string.IsNullOrWhiteSpace(l.ProductName)
                        ? UnnamedProductLabel
                        : l.ProductName.Trim();
                    productQty.TryGetValue(name, out var qty);
                    productQty[name] = qty + l.Quantity;
                }
            }

            return productQty;
        }

        private static decimal? ChangePercent(decimal current, decimal previous) =>
            previous == 0 ? (decimal?)null : Math.Round((current - previous) / previous * 100, 1);

        private static decimal? ChangePercent(long current, long previous) =>
            previous == 0 ? (decimal?)null : Math.Round((decimal)(current - previous) / previous * 100, 1);

        private static string ChangeSuffix(decimal? changePercent, string metric)
        {
            if (!changePercent.HasValue)
            {
                return $" (tuần trước chưa có {metric})";
            }

            var arrow = changePercent.Value > 0 ? "▲" : changePercent.Value < 0 ? "▼" : "＝";
            var sign = changePercent.Value > 0 ? "+" : "";
            return $" ({arrow}{sign}{changePercent.Value.ToString("N1", CultureInfo.InvariantCulture)}% so với tuần trước)";
        }

        private static string FormatVnd(decimal amount) =>
            $"{amount.ToString("N0", new CultureInfo("vi-VN"))}₫";

        private async Task<ShopSetting?> GetSingleSettingAsync()
        {
            var queryable = await _settings.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable);
        }

        private async Task MarkSentAsync(ShopSetting settings, string marker)
        {
            if (settings == null)
            {
                return;
            }

            settings.LastWeeklyReportSentWeek = marker;
            await _settings.UpdateAsync(settings, autoSave: true);
        }

        private string Unprotect(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            if (!stored.StartsWith(EncryptedPrefix, StringComparison.Ordinal))
            {
                return stored;
            }

            try
            {
                return _protector.Unprotect(stored.Substring(EncryptedPrefix.Length));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not decrypt the stored Zalo OA token.");
                return null;
            }
        }

        private sealed class WeekAggregate
        {
            public decimal Revenue { get; set; }
            public long OrderCount { get; set; }
            public long CancelledCount { get; set; }
            public List<WeeklyTopProductDto> TopProducts { get; set; } = new List<WeeklyTopProductDto>();
        }
    }
}
