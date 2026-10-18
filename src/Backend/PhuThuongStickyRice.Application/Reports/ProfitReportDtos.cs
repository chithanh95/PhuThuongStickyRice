using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Reports
{
        public class ProfitDayDto
        {
            public DateTime Date { get; set; }

            public decimal Revenue { get; set; }

            public decimal Cost { get; set; }

            public decimal Profit { get; set; }

            public long Orders { get; set; }
        }

        public class ProfitProductDto
        {
            public string ProductName { get; set; }

            public long QuantitySold { get; set; }

            public decimal Revenue { get; set; }

            public decimal Cost { get; set; }

            public decimal Profit { get; set; }
        }

        public class ProfitReportDto
        {
            public decimal TotalRevenue { get; set; }

            public decimal TotalCost { get; set; }

            public decimal TotalProfit { get; set; }

            public List<ProfitDayDto> ByDay { get; set; } = new List<ProfitDayDto>();

            public List<ProfitProductDto> ByProduct { get; set; } = new List<ProfitProductDto>();
        }
}
