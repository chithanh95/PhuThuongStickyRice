using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Dashboard.Models
{
    /// <summary>Item 138: one menu item's weekly sales.</summary>
    public class WeeklyTopProductModel
    {
        public string ProductName { get; set; }

        public long Quantity { get; set; }
    }

    /// <summary>Item 138: weekly owner report (GET api/admin/reports/weekly).</summary>
    public class WeeklyReportModel
    {
        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public string WeekLabel { get; set; }

        public decimal TotalRevenue { get; set; }

        public long OrderCount { get; set; }

        public long CancelledCount { get; set; }

        public decimal AverageOrderValue { get; set; }

        public List<WeeklyTopProductModel> TopProducts { get; set; } = new List<WeeklyTopProductModel>();

        public decimal PrevWeekRevenue { get; set; }

        public long PrevWeekOrderCount { get; set; }

        public decimal? RevenueChangePercent { get; set; }

        public decimal? OrderChangePercent { get; set; }

        public DateTime GeneratedAt { get; set; }

        /// <summary>The report rendered as plain text (Zalo message / copy-paste).</summary>
        public string MessageText { get; set; }
    }

    /// <summary>Item 138: result of POST api/admin/reports/weekly/send.</summary>
    public class WeeklyReportSendResultModel
    {
        public bool IsSent { get; set; }

        public bool ZaloConfigured { get; set; }

        public string MessageText { get; set; }

        public string Note { get; set; }
    }
}
