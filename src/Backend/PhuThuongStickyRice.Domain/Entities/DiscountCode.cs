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
        /// <summary>Độ dài tối đa của mã giảm giá.</summary>
        public const int CodeMaxLength = 50;

        /// <summary>Mã giảm giá (đã chuẩn hóa in hoa, duy nhất trong hệ thống).</summary>
        public string Code { get; set; }

        /// <summary>0-100. Null when the discount is a fixed amount.</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>Fixed-amount discount in VND. Null when percent-based.</summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>Giá trị đơn hàng tối thiểu (VND) để được áp mã.</summary>
        public decimal MinOrderAmount { get; set; }

        /// <summary>Thời điểm hết hạn (UTC); null = không giới hạn thời gian.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Số lượt dùng tối đa; null = không giới hạn số lần dùng.</summary>
        public int? MaxUses { get; set; }

        /// <summary>
        /// Số lượt đã dùng. Chỉ tăng sau khi đơn hàng tạo thành công —
        /// đơn lỗi (thanh toán hỏng, khách bỏ giỏ) không được đốt lượt mã.
        /// </summary>
        public int UsedCount { get; set; }

        /// <summary>True = mã đang bật, có thể dùng; false = đã bị tắt.</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Voucher type (item 120): Discount (giảm giá) or Freeship
        /// (miễn phí ship — sets the delivery fee to 0 at checkout).
        /// </summary>
        public VoucherType VoucherType { get; set; }

        /// <summary>
        /// Optional phone the code is tied to (lucky-spin / birthday vouchers).
        /// When set, only this phone may redeem the code.
        /// </summary>
        public string OwnerPhone { get; set; }
    }
}
