using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Reports
{
    /// <summary>
    /// Item 138: one menu item's weekly sales, for the top-5 list.
    /// </summary>
    public class WeeklyTopProductDto
    {
        public string ProductName { get; set; }

        public long Quantity { get; set; }
    }

    /// <summary>
    /// Item 138: aggregated numbers for one full Monday–Sunday week
    /// (Vietnam time), compared against the previous week.
    /// </summary>
    public class WeeklyReportDto
    {
        /// <summary>Monday of the reported week (Vietnam date).</summary>
        public DateTime WeekStart { get; set; }

        /// <summary>Sunday of the reported week (Vietnam date).</summary>
        public DateTime WeekEnd { get; set; }

        /// <summary>e.g. "Tuần 42 (13/10 – 19/10/2026)".</summary>
        public string WeekLabel { get; set; }

        /// <summary>Revenue from non-cancelled orders.</summary>
        public decimal TotalRevenue { get; set; }

        /// <summary>Non-cancelled order count.</summary>
        public long OrderCount { get; set; }

        public long CancelledCount { get; set; }

        public decimal AverageOrderValue { get; set; }

        public List<WeeklyTopProductDto> TopProducts { get; set; } = new List<WeeklyTopProductDto>();

        public decimal PrevWeekRevenue { get; set; }

        public long PrevWeekOrderCount { get; set; }

        /// <summary>Revenue change vs previous week, in percent. Null when
        /// the previous week had no revenue (nothing to compare).</summary>
        public decimal? RevenueChangePercent { get; set; }

        public decimal? OrderChangePercent { get; set; }

        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// The report rendered as plain text (Zalo message / copy-paste).
        /// Built server-side so every client copies the exact same text.
        /// </summary>
        public string MessageText { get; set; }
    }

    /// <summary>
    /// Item 138: outcome of a weekly-report delivery attempt. The report
    /// text is always returned so the admin can copy-paste it manually —
    /// the service never pretends a message was sent when it wasn't.
    /// </summary>
    public class WeeklyReportSendResultDto
    {
        /// <summary>True when the message was handed to the Zalo OA service
        /// with a configured token.</summary>
        public bool Sent { get; set; }

        /// <summary>True when a Zalo OA token + owner recipient are configured.</summary>
        public bool ZaloConfigured { get; set; }

        /// <summary>The full report text (for the "Sao chép" button).</summary>
        public string MessageText { get; set; }

        /// <summary>Human-readable explanation of what happened.</summary>
        public string Note { get; set; }
    }
}
