using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Orders
{
        /// <summary>
        /// Mục tiêu doanh thu ngày trên dashboard admin: doanh thu hôm nay
        /// so với mục tiêu đã đặt, kèm % hoàn thành (có thể vượt 100% khi
        /// bán vượt mục tiêu).
        /// </summary>
        public class DailyTargetDto
        {
            public decimal TodayRevenue { get; set; }
            public decimal Target { get; set; }

            /// <summary>0-100+ percent.</summary>
            public double Percent { get; set; }
        }

        /// <summary>
        /// Tổng hợp 1 nguồn đơn hàng trên dashboard (web, hotline, Zalo...):
        /// số đơn và doanh thu của nguồn đó trong kỳ báo cáo.
        /// </summary>
        public class SourceStatDto
        {
            public string Source { get; set; }
            public int OrderCount { get; set; }
            public decimal Revenue { get; set; }
        }

        /// <summary>
        /// So sánh hôm nay với hôm qua trên dashboard admin: doanh thu và
        /// số đơn của 2 ngày, kèm % tăng/giảm để chủ quán nắm xu hướng kinh
        /// doanh hàng ngày.
        /// </summary>
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
