using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PhuThuongStickyRice.Application.Reports
{
    /// <summary>
    /// Sự kiện funnel do client gửi về (theo dõi hành trình khách: xem landing → shop → giỏ → checkout → đặt đơn).
    /// </summary>
    public class TrackFunnelEventDto
    {
        /// <summary>
        /// Tên sự kiện — phải nằm trong allowlist (landing_view, shop_view, cart_view,
        /// checkout_view, order_placed, track_view); tên lạ bị bỏ qua.
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// URL trang phát sinh sự kiện. Chỉ lưu path (cắt query string/fragment) để không dính PII.
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// Số lượt sự kiện funnel theo tên: hôm nay + 7 ngày gần nhất (giờ Việt Nam).
    /// </summary>
    public class FunnelEventCountDto
    {
        /// <summary>Tên sự kiện (một trong các giá trị allowlist).</summary>
        public string EventName { get; set; }

        /// <summary>Số lượt trong hôm nay (tính theo giờ Việt Nam).</summary>
        public long Today { get; set; }

        /// <summary>Số lượt trong 7 ngày gần nhất (bao gồm hôm nay).</summary>
        public long Last7Days { get; set; }
    }

    public interface IFunnelEventAppService : IApplicationService
    {
        /// <summary>
        /// Anonymous funnel tracking. EventName must be in the allowlist;
        /// no PII is stored (URL is truncated, query strings stripped).
        /// </summary>
        Task TrackAsync(TrackFunnelEventDto input);

        /// <summary>Admin: counts per event for today + last 7 days (Vietnam time).</summary>
        Task<List<FunnelEventCountDto>> GetReportAsync();
    }

    /// <summary>
    /// Item funnel analytics: ghi nhận lượt xem trang theo hành trình mua hàng của khách
    /// (landing → shop → giỏ → checkout → đặt đơn → tra cứu) và báo cáo cho Admin.
    /// Không lưu PII — URL chỉ giữ path, sự kiện chỉ nhận theo allowlist.
    /// </summary>
    public class FunnelEventAppService : ApplicationService, IFunnelEventAppService
    {
        /// <summary>GL1 allowlist — the only event names the API accepts.</summary>
        public static readonly string[] AllowedEvents =
        {
            "landing_view",
            "shop_view",
            "cart_view",
            "checkout_view",
            "order_placed",
            "track_view",
        };

        /// <summary>Repository bảng FunnelEvent (lưu từng lượt sự kiện funnel).</summary>
        private readonly IRepository<FunnelEvent, Guid> _events;

        /// <summary>Số ngày trong cửa sổ báo cáo (bao gồm hôm nay).</summary>
        private const int ReportWindowDays = 7;

        /// <summary>Khởi tạo service theo dõi funnel.</summary>
        /// <param name="events">Repository bảng FunnelEvent.</param>
        public FunnelEventAppService(IRepository<FunnelEvent, Guid> events)
        {
            _events = events;
        }

        /// <summary>
        /// Ghi nhận một sự kiện funnel từ client (không yêu cầu đăng nhập).
        /// Tên sự kiện lạ bị bỏ qua im lặng (fail-soft) để không ảnh hưởng trải nghiệm khách.
        /// </summary>
        /// <param name="input">Tên sự kiện và URL trang phát sinh.</param>
        public virtual async Task TrackAsync(TrackFunnelEventDto input)
        {
            var name = (input?.EventName ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedEvents.Contains(name))
            {
                return; // fail-soft: unknown events are ignored, not errors
            }

            var url = (input?.Url ?? string.Empty).Trim();
            // No PII: keep path only, drop query string / fragment, cap length.
            var queryIndex = url.IndexOf('?');
            if (queryIndex >= 0) url = url[..queryIndex];
            var fragmentIndex = url.IndexOf('#');
            if (fragmentIndex >= 0) url = url[..fragmentIndex];
            if (url.Length > FunnelEvent.UrlMaxLength) url = url[..FunnelEvent.UrlMaxLength];

            await _events.AddAsync(new FunnelEvent
            {
                EventName = name,
                Url = url,
                CreatedDateTime = DateTimeOffset.UtcNow,
            });
        }

        /// <summary>
        /// Admin: báo cáo số lượt sự kiện hôm nay + 7 ngày gần nhất theo giờ Việt Nam.
        /// Luôn trả đủ 6 sự kiện trong allowlist (kể cả khi chưa có lượt nào).
        /// </summary>
        /// <returns>Số lượt theo từng tên sự kiện.</returns>
        public virtual async Task<List<FunnelEventCountDto>> GetReportAsync()
        {
            // Mốc ngày theo giờ Việt Nam (không có giờ mùa hè — offset lấy từ VietnamTime).
            var vnNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(VietnamTime.UtcOffsetHours));
            var todayStart = new DateTimeOffset(vnNow.Date, TimeSpan.FromHours(VietnamTime.UtcOffsetHours));
            // Cửa sổ báo cáo bao gồm cả hôm nay nên lùi (số ngày - 1) ngày từ đầu hôm nay.
            var weekStart = todayStart.AddDays(-(ReportWindowDays - 1));

            // perf-hunt 2026-10-19: aggregate in SQL (GROUP BY) instead of
            // loading every row of the last 7 days into memory. The table
            // grows one row per page view, so the in-memory version gets
            // heavier every day. Same semantics: per-event counts for the
            // 7-day window, with the today-subset counted conditionally.
            var grouped = await _events.ToListAsync(
                _events.GetAll()
                    .Where(e => e.CreatedDateTime >= weekStart)
                    .GroupBy(e => e.EventName)
                    .Select(g => new
                    {
                        EventName = g.Key,
                        Today = g.LongCount(e => e.CreatedDateTime >= todayStart),
                        Last7Days = g.LongCount(),
                    }));

            var byName = grouped.ToDictionary(x => x.EventName, x => x);

            return AllowedEvents
                .Select(name =>
                {
                    byName.TryGetValue(name, out var row);
                    return new FunnelEventCountDto
                    {
                        EventName = name,
                        Today = row?.Today ?? 0,
                        Last7Days = row?.Last7Days ?? 0,
                    };
                })
                .ToList();
        }
    }
}
