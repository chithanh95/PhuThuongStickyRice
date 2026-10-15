using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Admin.Models
{
    public class DiscountCodeModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal? DiscountAmount { get; set; }

        public decimal MinOrderAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int? MaxUses { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>0 = Discount (giảm giá), 1 = Freeship (miễn phí ship).</summary>
        public int VoucherType { get; set; }

        public string DisplayValue =>
            VoucherType == 1 ? "🚚 Miễn phí ship" :
            DiscountPercent.HasValue ? $"Giảm {DiscountPercent.Value:N0}%" :
            DiscountAmount.HasValue ? $"Giảm {DiscountAmount.Value:N0} ₫" : "—";
    }

    public class ValidateDiscountResultModel
    {
        public bool Valid { get; set; }

        public string Message { get; set; }

        public decimal DiscountAmount { get; set; }

        public bool IsFreeship { get; set; }
    }

    public class CustomerModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public long OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime LastOrderAt { get; set; }

        /// <summary>Item 149: tags attached to this customer phone.</summary>
        public List<CustomerTagModel> Tags { get; set; } = new List<CustomerTagModel>();

        /// <summary>
        /// Item 130 (đúng): true when the phone carries the VIP tag.
        /// </summary>
        public bool IsVip { get; set; }
    }

    /// <summary>Item 149: customer tag (VIP, Nợ, Khó tính...).</summary>
    public class CustomerTagModel
    {
        public Guid Id { get; set; }

        public string CustomerPhone { get; set; }

        public string TagName { get; set; }

        public string Color { get; set; }

        public string CreatedBy { get; set; }
    }

    /// <summary>Item 168: detailed purchase history of one customer.</summary>
    public class CustomerDetailModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public long TotalOrders { get; set; }

        public long CancelledOrders { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime? FirstOrderAt { get; set; }

        public DateTime? LastOrderAt { get; set; }

        public List<CustomerTopProductModel> TopProducts { get; set; }
            = new List<CustomerTopProductModel>();

        public List<CustomerRecentOrderModel> RecentOrders { get; set; }
            = new List<CustomerRecentOrderModel>();

        public List<CustomerTagModel> Tags { get; set; }
            = new List<CustomerTagModel>();
    }

    public class CustomerTopProductModel
    {
        public string ProductName { get; set; }

        public long Quantity { get; set; }
    }

    public class CustomerRecentOrderModel
    {
        public string OrderNumber { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; }

        public string StatusLabel { get; set; }

        public string PaymentMethodLabel { get; set; }
    }

    /// <summary>Item 169: revenue summary of one date range.</summary>
    public class RevenueSummaryModel
    {
        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public long OrderCount { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal AvgOrderValue { get; set; }

        public List<RevenueSummaryTopProductModel> TopProducts { get; set; }
            = new List<RevenueSummaryTopProductModel>();
    }

    public class RevenueSummaryTopProductModel
    {
        public string ProductName { get; set; }

        public long Quantity { get; set; }

        public decimal Revenue { get; set; }
    }

    public class TopProductModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }
}
