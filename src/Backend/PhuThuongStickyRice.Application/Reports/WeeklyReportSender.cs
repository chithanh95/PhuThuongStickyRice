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
    /// Item 138: dựng và gửi báo cáo tuần sáng Thứ Hai cho chủ quán. Cố ý KHÔNG
    /// kế thừa <c>ApplicationService</c> và không gắn <c>[Authorize]</c> để
    /// worker BackgroundServer (không cần đăng nhập) có thể gọi; mặt HTTP
    /// (<see cref="IWeeklyReportAppService"/>) chỉ cho Admin.
    /// </summary>
    public interface IWeeklyReportSender : ITransientDependency
    {
        /// <summary>Tổng hợp số liệu của tuần đầy đủ Thứ Hai–Chủ Nhật vừa rồi (giờ Việt Nam).</summary>
        /// <returns>DTO số liệu tuần.</returns>
        Task<WeeklyReportDto> BuildReportAsync();

        /// <summary>Render báo cáo thành text thuần cho Zalo / copy tay.</summary>
        /// <param name="report">Số liệu tuần đã tổng hợp.</param>
        /// <returns>Nội dung text của báo cáo.</returns>
        string BuildMessageText(WeeklyReportDto report);

        /// <summary>
        /// Dựng báo cáo, gửi qua Zalo OA khi đã cấu hình, và lưu marker chống
        /// gửi trùng. Không ném lỗi khi thiếu token — trả về <c>Sent = false</c>
        /// kèm text để copy thủ công.
        /// </summary>
        /// <returns>Kết quả gửi (đã gửi hay chưa + nội dung).</returns>
        Task<WeeklyReportSendResultDto> SendAsync();
    }

    /// <summary>
    /// Hiện thực dựng và gửi báo cáo tuần (item 138): gom đơn của tuần vừa rồi
    /// theo giờ Việt Nam, so sánh với tuần trước, rồi gửi qua Zalo OA. Token
    /// Zalo đọc từ ShopSetting dưới dạng mã hóa (DataProtection), giải mã
    /// trước khi gửi.
    /// </summary>
    public partial class WeeklyReportSender : IWeeklyReportSender
    {
        /// <summary>Số ngày trong một tuần — dùng khi tính khoảng tuần trước/sau.</summary>
        private const int DaysPerWeek = 7;

        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<ShopSetting, Guid> _settings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IZaloNotificationService _zalo;
        private readonly IDataProtector _protector;
        private readonly ILogger<WeeklyReportSender> _logger;

        /// <summary>
        /// Khởi tạo bộ dựng báo cáo tuần: truy vấn đơn hàng, đọc token Zalo mã
        /// hóa trong ShopSetting, gửi qua Zalo OA và ghi log.
        /// </summary>
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
            // there can be decrypted here (xem Common.ProtectedSecrets).
            _protector = dataProtectionProvider.CreateProtector(ProtectedSecrets.Purpose);
            _logger = logger;
        }

        /// <summary>
        /// Tuần đầy đủ Thứ Hai–Chủ Nhật gần nhất (theo ngày Việt Nam) tính từ
        /// <paramref name="vietnamToday"/>. Vào sáng Thứ Hai thì đây là tuần vừa kết thúc.
        /// </summary>
        /// <param name="vietnamToday">Ngày hôm nay theo giờ Việt Nam.</param>
        /// <returns>Ngày bắt đầu (Thứ Hai) và ngày kết thúc (Chủ Nhật) của tuần.</returns>
        public static (DateTime Start, DateTime End) LastFullWeek(DateTime vietnamToday)
        {
            var today = vietnamToday.Date;
            var daysSinceMonday = ((int)today.DayOfWeek + (DaysPerWeek - 1)) % DaysPerWeek;
            var start = today.AddDays(-daysSinceMonday - DaysPerWeek);
            return (start, start.AddDays(DaysPerWeek - 1));
        }

        /// <summary>
        /// Marker chống gửi trùng tuần, lưu ở ShopSetting — tuần nào đã gửi thì
        /// worker Thứ Hai bỏ qua, không gửi lại.
        /// </summary>
        /// <param name="weekStart">Ngày bắt đầu tuần.</param>
        /// <returns>Chuỗi marker theo dạng yyyy-MM-dd.</returns>
        public static string WeekMarker(DateTime weekStart) =>
            weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>
        /// Dựng số liệu tuần đầy đủ vừa rồi và so sánh với tuần trước đó để
        /// tính % thay đổi doanh thu / số đơn.
        /// </summary>
        /// <returns>DTO số liệu tuần kèm nội dung text gửi Zalo.</returns>
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

        /// <summary>
        /// Vẽ báo cáo thành văn bản thuần để gửi Zalo OA hoặc chủ quán copy:
        /// doanh thu, số đơn, đơn hủy, TB/đơn kèm % thay đổi so với tuần trước
        /// và top món bán chạy. Xem <see cref="BuildReportAsync"/> để biết nguồn số liệu.
        /// </summary>
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
            // CA1305: chỉ rõ culture tường minh (giữ CurrentCulture = đúng
            // behavior cũ, tránh phụ thuộc locale máy chủ một cách ngầm định).
            sb.AppendLine(CultureInfo.CurrentCulture, $"❌ Đơn hủy: {report.CancelledCount:N0}");
            sb.AppendLine(CultureInfo.CurrentCulture, $"🧮 Giá trị TB/đơn: {FormatVnd(report.AverageOrderValue)}");
            sb.AppendLine();
            if (report.TopProducts.Count > 0)
            {
                sb.AppendLine("🏆 Món bán chạy:");
                // Thứ hạng món bán chạy (1 = bán chạy nhất tuần).
                var rank = 1;
                foreach (var topProduct in report.TopProducts)
                {
                    sb.AppendLine(CultureInfo.CurrentCulture, $"{rank}. {topProduct.ProductName} — {topProduct.Quantity:N0} suất");
                    rank++;
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

        /// <summary>
        /// Gửi báo cáo tuần qua Zalo OA cho chủ quán. Chưa cấu hình token thì
        /// vẫn đánh dấu tuần đã xử lý (để worker Thứ Hai không retry liên tục
        /// suốt buổi sáng) và trả nội dung text để admin copy gửi tay — không
        /// "giả vờ" đã gửi.
        /// </summary>
        /// <returns>Kết quả gửi kèm nội dung báo cáo.</returns>
        public async Task<WeeklyReportSendResultDto> SendAsync()
        {
            var report = await BuildReportAsync();
            var message = BuildMessageText(report);
            var marker = WeekMarker(report.WeekStart);

            var settings = await GetSingleSettingAsync();
            var token = Unprotect(settings?.ZaloOaAccessToken);
            // Ưu tiên gửi tới Zalo User ID đã lưu; chưa có thì dùng SĐT chủ quán.
            var recipient = string.IsNullOrWhiteSpace(settings?.OwnerZaloUserId)
                ? settings?.OwnerPhone?.Trim()
                : settings.OwnerZaloUserId.Trim();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(recipient))
            {
                // Vẫn đánh dấu tuần đã xử lý để worker Thứ Hai không retry
                // mỗi 10 phút suốt buổi sáng chỉ vì thiếu cấu hình.
                await MarkSentAsync(settings, marker);
                LogZaloNotConfigured(_logger, marker);
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
            LogReportHandedToZalo(_logger, marker, recipient);

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

        /// <summary>
        /// Gom số liệu của 1 tuần theo giờ Việt Nam: đổi khoảng [start, end] ngày
        /// VN sang UTC để so với cột <c>CreatedAt</c> (lưu UTC), lấy các dòng
        /// đơn rút gọn rồi tổng hợp doanh thu / số đơn / top món. Đơn hủy không
        /// tính vào doanh thu nhưng vẫn đếm riêng để chủ quán biết.
        /// </summary>
        /// <param name="weekStartVn">Ngày đầu tuần (Thứ Hai, giờ VN).</param>
        /// <param name="weekEndVn">Ngày cuối tuần (Chủ Nhật, giờ VN).</param>
        /// <returns>Số liệu tổng hợp của tuần.</returns>
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
            foreach (var orderRow in billable)
            {
                foreach (var line in orderRow.Lines)
                {
                    var name = string.IsNullOrWhiteSpace(line.ProductName)
                        ? UnnamedProductLabel
                        : line.ProductName.Trim();
                    productQty.TryGetValue(name, out var qty);
                    productQty[name] = qty + line.Quantity;
                }
            }

            return productQty;
        }

        /// <summary>
        /// % thay đổi của tuần này so với tuần trước, làm tròn 1 số lẻ. Tuần
        /// trước = 0 thì trả null (không thể chia) — người đọc sẽ thấy chữ
        /// "tuần trước chưa có dữ liệu" thay vì con số vô nghĩa.
        /// </summary>
        private static decimal? ChangePercent(decimal current, decimal previous) =>
            previous == 0 ? (decimal?)null : Math.Round((current - previous) / previous * 100, 1);

        /// <summary>% thay đổi cho chỉ số nguyên (số đơn); tuần trước = 0 → null.</summary>
        private static decimal? ChangePercent(long current, long previous) =>
            previous == 0 ? (decimal?)null : Math.Round((decimal)(current - previous) / previous * 100, 1);

        /// <summary>Hậu tố "% so với tuần trước" với mũi tên ▲/▼ cho báo cáo text.</summary>
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

        /// <summary>Định dạng số tiền VNĐ (phân cách hàng nghìn, ký hiệu ₫).</summary>
        private static string FormatVnd(decimal amount) =>
            $"{amount.ToString("N0", VietnameseCulture.Value)}₫";

        /// <summary>Đọc bản ghi ShopSetting duy nhất (chứa token Zalo, SĐT chủ quán).</summary>
        private async Task<ShopSetting?> GetSingleSettingAsync()
        {
            var queryable = await _settings.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(queryable);
        }

        /// <summary>
        /// Đánh dấu tuần đã xử lý vào ShopSetting để worker Thứ Hai không gửi
        /// trùng. Thiếu bản ghi setting thì bỏ qua (không lỗi).
        /// </summary>
        private async Task MarkSentAsync(ShopSetting settings, string marker)
        {
            if (settings == null)
            {
                return;
            }

            settings.LastWeeklyReportSentWeek = marker;
            await _settings.UpdateAsync(settings, autoSave: true);
        }

        // [codequality-smell 2026-10-20] Logic giải mã gom về
        // Common.ProtectedSecrets dùng chung với ShopSettingAppService —
        // trước đây 2 service copy nguyên khối try/catch + tiền tố DPv1:.
        /// <summary>
        /// Giải mã token Zalo đã lưu ở ShopSetting (DataProtection, tiền tố
        /// "DPv1:"). Giá trị cũ lưu plaintext (không tiền tố) trả về nguyên —
        /// tương thích dữ liệu trước khi có mã hóa. Giải mã lỗi → null và ghi
        /// warning, không ném (báo cáo vẫn gửi được ở dạng copy tay).
        /// </summary>
        /// <param name="stored">Chuỗi token đã lưu (mã hóa hoặc plaintext).</param>
        /// <returns>Token thật để gọi Zalo API, hoặc null khi không giải được.</returns>
        private string? Unprotect(string? stored)
            => ProtectedSecrets.Unprotect(_protector, stored, ex => LogZaloTokenDecryptFailed(_logger, ex));

        private sealed class WeekAggregate
        {
            public decimal Revenue { get; set; }
            public long OrderCount { get; set; }
            public long CancelledCount { get; set; }
            public List<WeeklyTopProductDto> TopProducts { get; set; } = new List<WeeklyTopProductDto>();
        }

        // CA1848/CA1873: logging hiệu năng cao bằng LoggerMessage
        // source-generated (không cấp phát params array khi log tắt).

        /// <summary>Log khi Zalo OA chưa cấu hình — trả text để admin gửi tay.</summary>
        [LoggerMessage(Level = LogLevel.Information, Message = "Weekly report {Week}: Zalo OA not configured — returning text for manual copy.")]
        private static partial void LogZaloNotConfigured(ILogger logger, string week);

        /// <summary>Log khi báo cáo đã chuyển cho dịch vụ Zalo OA.</summary>
        [LoggerMessage(Level = LogLevel.Information, Message = "Weekly report {Week} handed to the Zalo OA service for {Recipient}.")]
        private static partial void LogReportHandedToZalo(ILogger logger, string week, string recipient);

        /// <summary>Log khi không giải mã được token Zalo OA đã lưu.</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "Could not decrypt the stored Zalo OA token.")]
        private static partial void LogZaloTokenDecryptFailed(ILogger logger, Exception ex);
    }
}
