using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Reports
{
        /// <summary>
        /// Lợi nhuận một ngày: doanh thu, chi phí (nguyên liệu ước tính),
        /// lợi nhuận (doanh thu − chi phí) và số đơn hàng trong ngày (VNĐ).
        /// </summary>
        public class ProfitDayDto
        {
            public DateTime Date { get; set; }

            public decimal Revenue { get; set; }

            public decimal Cost { get; set; }

            public decimal Profit { get; set; }

            public long Orders { get; set; }
        }

        /// <summary>
        /// Lợi nhuận theo sản phẩm: số lượng đã bán, doanh thu, chi phí
        /// (nguyên liệu ước tính) và lợi nhuận của món (VNĐ).
        /// </summary>
        public class ProfitProductDto
        {
            public string ProductName { get; set; }

            public long QuantitySold { get; set; }

            public decimal Revenue { get; set; }

            public decimal Cost { get; set; }

            public decimal Profit { get; set; }
        }

        /// <summary>
        /// Báo cáo lợi nhuận tổng hợp (màn hình admin): tổng doanh thu, tổng
        /// chi phí, tổng lợi nhuận (VNĐ), chi tiết theo ngày và theo sản phẩm.
        /// </summary>
        public class ProfitReportDto
        {
            public decimal TotalRevenue { get; set; }

            public decimal TotalCost { get; set; }

            public decimal TotalProfit { get; set; }

            public List<ProfitDayDto> ByDay { get; set; } = new List<ProfitDayDto>();

            public List<ProfitProductDto> ByProduct { get; set; } = new List<ProfitProductDto>();
        }
}
