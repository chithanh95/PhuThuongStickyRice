using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Discount / promo code. Either <see cref="DiscountPercent"/> or
    /// <see cref="DiscountAmount"/> is set (percent wins if both are set).
    /// Amounts are in VND.
    /// </summary>
    public class DiscountCode : Entity<Guid>, IAggregateRoot
    {
        public const int CodeMaxLength = 50;

        public string Code { get; set; }

        /// <summary>0-100. Null when the discount is a fixed amount.</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>Fixed-amount discount in VND. Null when percent-based.</summary>
        public decimal? DiscountAmount { get; set; }

        public decimal MinOrderAmount { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int? MaxUses { get; set; }

        public int UsedCount { get; set; }

        public bool IsActive { get; set; }
    }
}
