using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Mã giảm giá / khuyến mãi. Một mã có <see cref="DiscountPercent"/>
    /// hoặc <see cref="DiscountAmount"/> (nếu có cả hai thì % thắng).
    /// Mọi số tiền tính bằng VND.
    /// </summary>
    public class DiscountCode : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Độ dài tối đa của mã giảm giá.</summary>
        public const int CodeMaxLength = 50;

        /// <summary>Mã giảm giá (đã chuẩn hóa in hoa, duy nhất trong hệ thống).</summary>
        public string Code { get; set; }

        /// <summary>Phần trăm giảm (0-100). Null khi mã giảm số tiền cố định.</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>Số tiền giảm cố định (VND). Null khi mã giảm theo phần trăm.</summary>
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
        /// Loại voucher (item 120): Discount (giảm giá) hoặc Freeship
        /// (miễn phí ship — phí giao hàng về 0 ở checkout).
        /// </summary>
        public VoucherType VoucherType { get; set; }

        /// <summary>
        /// SĐT gắn với mã (voucher vòng quay may mắn / sinh nhật).
        /// Khi có giá trị, chỉ SĐT này mới được dùng mã.
        /// </summary>
        public string OwnerPhone { get; set; }
    }
}
