using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// Hourly best-sellers report for one Vietnam-local day.
    /// Heatmap source: rows = hours 6h–22h, columns = products.
    /// </summary>
    public class HourlySalesReportDto
    {
        /// <summary>Vietnam-local date (date only).</summary>
        public DateTime Date { get; set; }

        /// <summary>Product names, ordered by total quantity desc.</summary>
        public List<string> Products { get; set; } = new List<string>();

        /// <summary>One row per hour (6..22). Quantities aligned with <see cref="Products"/>.</summary>
        public List<HourlySalesRowDto> Hours { get; set; } = new List<HourlySalesRowDto>();

        /// <summary>Top 3 busiest hours by portions sold.</summary>
        public List<TopHourDto> TopHours { get; set; } = new List<TopHourDto>();

        public long TotalOrders { get; set; }

        public long TotalPortions { get; set; }

        /// <summary>Vietnamese preparation suggestion, e.g. "chuẩn bị nhiều xôi trước 7h".</summary>
        public string Suggestion { get; set; }
    }

    public class HourlySalesRowDto
    {
        public int Hour { get; set; }

        /// <summary>Quantities per product, same order as the report's Products.</summary>
        public List<long> Quantities { get; set; } = new List<long>();

        public long Total { get; set; }
    }

    public class TopHourDto
    {
        public int Hour { get; set; }

        public long Portions { get; set; }

        public long Orders { get; set; }
    }
}
