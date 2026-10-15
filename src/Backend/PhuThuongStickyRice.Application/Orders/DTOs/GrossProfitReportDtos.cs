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

    public class GrossProfitRowDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }

        /// <summary>Profit / Revenue × 100. Null when revenue is 0.</summary>
        public decimal? MarginPercent { get; set; }

        /// <summary>Current cost price used for this row.</summary>
        public decimal CostPrice { get; set; }

        /// <summary>True when no cost price was entered (profit = revenue).</summary>
        public bool CostMissing { get; set; }
    }

    public class GrossProfitTotalDto
    {
        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }

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
