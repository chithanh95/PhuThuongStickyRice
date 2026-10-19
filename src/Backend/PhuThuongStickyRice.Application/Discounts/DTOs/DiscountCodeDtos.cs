using PhuThuongStickyRice.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Discounts.DTOs
{
    /// <summary>
    /// Mã giảm giá hiển thị cho admin và checkout: mã, % hoặc số tiền giảm,
    /// đơn tối thiểu, hạn dùng, số lượt dùng và loại voucher (giảm giá/freeship).
    /// </summary>
    public class DiscountCodeDto
    {
        /// <summary>Id mã giảm giá (khóa chính GUID).</summary>
        public Guid Id { get; set; }

        /// <summary>Mã giảm giá (đã chuẩn hóa in hoa, duy nhất, tối đa 50 ký tự).</summary>
        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        /// <summary>0-100. Null when fixed-amount.</summary>
        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        /// <summary>Fixed VND amount. Null when percent-based.</summary>
        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        /// <summary>Giá trị đơn hàng tối thiểu (VNĐ) để được áp mã.</summary>
        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; }

        /// <summary>Thời điểm hết hạn (UTC); null = không giới hạn thời gian.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Số lượt dùng tối đa; null = không giới hạn số lần dùng.</summary>
        [Range(1, int.MaxValue)]
        public int? MaxUses { get; set; }

        /// <summary>Số lượt đã dùng (chỉ tăng sau khi đơn hàng tạo thành công).</summary>
        public int UsedCount { get; set; }

        /// <summary>True = mã đang bật, có thể dùng; false = đã bị tắt.</summary>
        public bool IsActive { get; set; }

        /// <summary>Phone the code is tied to (lucky-spin / birthday vouchers).</summary>
        public string OwnerPhone { get; set; }

        /// <summary>Voucher type (item 120): Discount or Freeship.</summary>
        public VoucherType VoucherType { get; set; }
    }

    /// <summary>
    /// Internal: creates a system-generated single-use code (lucky spin,
    /// birthday voucher). Called from other application services, not exposed
    /// directly as an API.
    /// </summary>
    public class CreateGeneratedDiscountDto
    {
        /// <summary>Tiền tố mã sinh tự động (vd "THUONG" → "THUONG-AB12CD"); trống thì dùng "VOUCHER".</summary>
        public string Prefix { get; set; }

        /// <summary>0-100. Null when fixed-amount. % có 1 trong 2 mới hợp lệ.</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>Số tiền giảm cố định (VNĐ); null khi giảm theo %.</summary>
        public decimal? DiscountAmount { get; set; }

        /// <summary>Giá trị đơn tối thiểu (VNĐ) để áp mã.</summary>
        public decimal MinOrderAmount { get; set; }

        /// <summary>Thời điểm hết hạn (UTC); null = không hết hạn.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>SĐT chủ sở hữu mã — chỉ SĐT này được dùng mã.</summary>
        public string OwnerPhone { get; set; }
    }

    /// <summary>
    /// Input admin tạo/sửa mã giảm giá: mã, giá trị giảm, đơn tối thiểu, hạn
    /// dùng, số lượt dùng tối đa, trạng thái và loại voucher.
    /// Freeship được miễn kiểm tra %/số tiền vì lợi ích là miễn phí ship.
    /// </summary>
    public class CreateDiscountCodeDto
    {
        /// <summary>Mã giảm giá (bắt buộc, tối đa 50 ký tự; lưu dạng in hoa).</summary>
        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        /// <summary>% giảm (0–100); bỏ trống khi giảm số tiền cố định.</summary>
        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        /// <summary>Số tiền giảm cố định (VNĐ); bỏ trống khi giảm theo %.</summary>
        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        /// <summary>Giá trị đơn tối thiểu (VNĐ) để được áp mã.</summary>
        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; }

        /// <summary>Thời điểm hết hạn (UTC); null = không hết hạn.</summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Số lượt dùng tối đa; null = không giới hạn.</summary>
        [Range(1, int.MaxValue)]
        public int? MaxUses { get; set; }

        /// <summary>True = bật ngay sau khi lưu (mặc định bật).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Voucher type (item 120): Discount (giảm giá) or Freeship (miễn phí ship).</summary>
        public VoucherType VoucherType { get; set; } = VoucherType.Discount;
    }

    /// <summary>
    /// Input kiểm tra 1 mã với tổng tiền giỏ hàng — trả kết quả và số tiền
    /// giảm nhưng CHƯA tăng lượt dùng (chỉ tính thử ở checkout).
    /// </summary>
    public class ValidateDiscountInput
    {
        /// <summary>Mã giảm giá khách nhập (so khớp không phân biệt hoa/thường).</summary>
        [Required]
        public string Code { get; set; }

        /// <summary>Tổng tiền hàng của giỏ (VNĐ) để kiểm tra đơn tối thiểu.</summary>
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Optional: required when the code is tied to a phone (lucky-spin /
        /// birthday vouchers).
        /// </summary>
        public string CustomerPhone { get; set; }
    }

    /// <summary>Kết quả kiểm tra 1 mã giảm giá cho checkout.</summary>
    public class ValidateDiscountResultDto
    {
        /// <summary>True khi mã dùng được với giỏ hàng hiện tại.</summary>
        public bool Valid { get; set; }

        /// <summary>Message tiếng Việt hiển thị trực tiếp cho khách (thành công hoặc lý do không dùng được).</summary>
        public string Message { get; set; }

        /// <summary>Discount in VND for the given subtotal (0 when invalid).</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>True when the code is a freeship voucher: fee becomes 0.</summary>
        public bool IsFreeship { get; set; }
    }

    /// <summary>
    /// perf-hunt 2026-10-19: batch input for checkout auto-apply — finds the
    /// best money-off public code in ONE server round-trip instead of
    /// GetActivePublicAsync + N x ValidateAsync.
    /// </summary>
    public class EvaluateBestDiscountInput
    {
        /// <summary>Tổng tiền hàng của giỏ (VNĐ) để tính mã tốt nhất.</summary>
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        /// <summary>Optional: owner-phone check for phone-tied codes.</summary>
        public string CustomerPhone { get; set; }
    }

    /// <summary>Best applicable money-off code, or null Code when none.</summary>
    public class BestDiscountResultDto
    {
        /// <summary>Mã giảm tốt nhất tìm được; null khi không có mã nào dùng được.</summary>
        public string Code { get; set; }

        /// <summary>Số tiền (VNĐ) mã tốt nhất giảm được (0 khi không có mã).</summary>
        public decimal DiscountAmount { get; set; }
    }
}
