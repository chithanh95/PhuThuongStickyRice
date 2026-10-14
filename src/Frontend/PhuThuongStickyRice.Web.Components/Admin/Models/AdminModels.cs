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

    public class AuditLogEntryModel
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Action { get; set; }

        public string ObjectId { get; set; }

        public string Log { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
    }

    public class CustomerModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public long OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime LastOrderAt { get; set; }
    }

    public class TopProductModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }
}
