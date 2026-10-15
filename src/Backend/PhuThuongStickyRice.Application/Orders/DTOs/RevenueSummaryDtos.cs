using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// Item 169: revenue summary of one Vietnam date range (cancelled orders
    /// excluded). The compare view calls this endpoint twice — once per
    /// period — so no compare-specific backend logic is needed.
    /// </summary>
    public class RevenueSummaryDto
    {
        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public long OrderCount { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal AvgOrderValue { get; set; }

        public List<RevenueSummaryTopProductDto> TopProducts { get; set; }
            = new List<RevenueSummaryTopProductDto>();
    }

    public class RevenueSummaryTopProductDto
    {
        public string ProductName { get; set; }

        public long Quantity { get; set; }

        public decimal Revenue { get; set; }
    }
}
