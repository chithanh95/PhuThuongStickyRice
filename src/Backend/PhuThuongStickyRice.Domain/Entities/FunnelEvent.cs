using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// GL1: funnel measurement — one row per page view of a key funnel step.
    /// No PII: only the event name (allowlisted) and the page URL path.
    /// </summary>
    public class FunnelEvent : Entity<Guid>, IAggregateRoot
    {
        public const int EventNameMaxLength = 100;
        public const int UrlMaxLength = 500;

        /// <summary>Allowlisted event name, e.g. "shop_view".</summary>
        public string EventName { get; set; }

        /// <summary>Page URL (path only, no query PII).</summary>
        public string Url { get; set; }
    }
}
