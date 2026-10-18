using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        public class DailyTargetDto
        {
            public decimal TodayRevenue { get; set; }
            public decimal Target { get; set; }

            /// <summary>0-100+ percent.</summary>
            public double Percent { get; set; }
        }

        public class SourceStatDto
        {
            public string Source { get; set; }
            public int OrderCount { get; set; }
            public decimal Revenue { get; set; }
        }

        public class TodayVsYesterdayDto
        {
            public decimal TodayRevenue { get; set; }
            public decimal YesterdayRevenue { get; set; }
            public long TodayOrders { get; set; }
            public long YesterdayOrders { get; set; }

            /// <summary>Revenue delta %, signed. Null-safe: 100 when yesterday = 0 and today > 0.</summary>
            public double RevenueDeltaPercent { get; set; }

            /// <summary>Order-count delta %, signed.</summary>
            public double OrdersDeltaPercent { get; set; }
        }
}
