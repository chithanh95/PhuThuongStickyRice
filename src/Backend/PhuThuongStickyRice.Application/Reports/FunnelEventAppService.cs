using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace PhuThuongStickyRice.Application.Reports
{
    public class TrackFunnelEventDto
    {
        public string EventName { get; set; }
        public string Url { get; set; }
    }

    public class FunnelEventCountDto
    {
        public string EventName { get; set; }
        public long Today { get; set; }
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

        private readonly IRepository<FunnelEvent, Guid> _events;

        public FunnelEventAppService(IRepository<FunnelEvent, Guid> events)
        {
            _events = events;
        }

        public virtual async Task TrackAsync(TrackFunnelEventDto input)
        {
            var name = (input?.EventName ?? string.Empty).Trim().ToLowerInvariant();
            if (!AllowedEvents.Contains(name))
            {
                return; // fail-soft: unknown events are ignored, not errors
            }

            var url = (input?.Url ?? string.Empty).Trim();
            // No PII: keep path only, drop query string / fragment, cap length.
            var q = url.IndexOf('?');
            if (q >= 0) url = url.Substring(0, q);
            var h = url.IndexOf('#');
            if (h >= 0) url = url.Substring(0, h);
            if (url.Length > FunnelEvent.UrlMaxLength) url = url.Substring(0, FunnelEvent.UrlMaxLength);

            await _events.AddAsync(new FunnelEvent
            {
                EventName = name,
                Url = url,
                CreatedDateTime = DateTimeOffset.UtcNow,
            });
        }

        public virtual async Task<List<FunnelEventCountDto>> GetReportAsync()
        {
            // Vietnam day boundaries (UTC+7, no DST).
            var vnNow = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
            var todayStart = new DateTimeOffset(vnNow.Date, TimeSpan.FromHours(7));
            var weekStart = todayStart.AddDays(-6);

            var rows = await _events.ToListAsync(
                _events.GetAll().Where(e => e.CreatedDateTime >= weekStart));

            return AllowedEvents
                .Select(name => new FunnelEventCountDto
                {
                    EventName = name,
                    Today = rows.LongCount(r => r.EventName == name && r.CreatedDateTime >= todayStart),
                    Last7Days = rows.LongCount(r => r.EventName == name),
                })
                .ToList();
        }
    }
}
