using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    /// <summary>
    /// Item 154: gross profit per product — revenue − cost, where cost is
    /// valued at each product's CURRENT CostPrice (no cost history is kept).
    /// </summary>
    public class GrossProfitReportDto
    {
        /// <summary>Range start (Vietnam calendar date, inclusive).</summary>
        public DateTime From { get; set; }

        /// <summary>Range end (Vietnam calendar date, inclusive).</summary>
        public DateTime To { get; set; }

        /// <summary>One row per sold product, sorted by profit desc.</summary>
        public List<GrossProfitRowDto> Rows { get; set; } = new List<GrossProfitRowDto>();

        public GrossProfitTotalDto Total { get; set; } = new GrossProfitTotalDto();

        /// <summary>Auto-generated business suggestions (Vietnamese).</summary>
        public List<string> Suggestions { get; set; } = new List<string>();

        /// <summary>True when at least one sold product has no cost price set.</summary>
        public bool HasUnpricedProducts { get; set; }

        /// <summary>Honest note about the cost basis, shown in the UI.</summary>
        public string CostBasisNote { get; set; }
    }

    /// <summary>
    /// 1 dòng lãi gộp của 1 món trong kỳ báo cáo: doanh thu − giá vốn.
    /// Lưu ý: giá vốn tính theo CostPrice HIỆN TẠI của món (không lưu lịch sử
    /// giá vốn), nên số liệu các kỳ cũ có thể lệch nếu sau đó admin sửa giá vốn.
    /// </summary>
    public class GrossProfitRowDto
    {
        public Guid ProductId { get; set; }

        /// <summary>Tên món tại thời điểm báo cáo.</summary>
        public string ProductName { get; set; }

        /// <summary>Tổng số phần đã bán trong kỳ.</summary>
        public long QuantitySold { get; set; }

        /// <summary>Tổng doanh thu của món trong kỳ (đ, sau giảm giá dòng).</summary>
        public decimal Revenue { get; set; }

        /// <summary>Tổng giá vốn = QuantitySold × CostPrice hiện tại (đ).</summary>
        public decimal Cost { get; set; }

        /// <summary>Lãi gộp = Revenue − Cost (đ).</summary>
        public decimal Profit { get; set; }

        /// <summary>Profit / Revenue × 100. Null when revenue is 0.</summary>
        public decimal? MarginPercent { get; set; }

        /// <summary>Current cost price used for this row.</summary>
        public decimal CostPrice { get; set; }

        /// <summary>True when no cost price was entered (profit = revenue).</summary>
        public bool CostMissing { get; set; }
    }

    /// <summary>
    /// Dòng tổng cộng của báo cáo lãi gộp — cộng dồn toàn bộ các dòng món
    /// trong kỳ (doanh thu, giá vốn, lãi gộp, biên lãi chung).
    /// </summary>
    public class GrossProfitTotalDto
    {
        /// <summary>Tổng số phần đã bán của mọi món trong kỳ.</summary>
        public long QuantitySold { get; set; }

        /// <summary>Tổng doanh thu mọi món trong kỳ (đ).</summary>
        public decimal Revenue { get; set; }

        /// <summary>Tổng giá vốn mọi món trong kỳ (đ).</summary>
        public decimal Cost { get; set; }

        /// <summary>Tổng lãi gộp = Revenue − Cost (đ).</summary>
        public decimal Profit { get; set; }

        /// <summary>Biên lãi gộp chung = Profit / Revenue × 100; null khi doanh thu = 0.</summary>
        public decimal? MarginPercent { get; set; }
    }

    /// <summary>Product with its current cost price — feeds the cost editor UI.</summary>
    public class ProductCostDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Unit { get; set; }

        public decimal Price { get; set; }

        public decimal CostPrice { get; set; }
    }
}
