using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Loyalty account keyed by phone number. Tracks points earned on
    /// completed orders, referral codes, birthday vouchers and lifetime
    /// spend (used for membership tiers).
    /// </summary>
    public class LoyaltyAccount : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Độ dài tối đa cột SĐT trong DB.</summary>
        public const int PhoneMaxLength = 30;
        /// <summary>Độ dài tối đa cột mã giới thiệu trong DB.</summary>
        public const int ReferralCodeMaxLength = 20;

        /// <summary>Phone number, digits only where possible. Unique.</summary>
        public string Phone { get; set; }

        /// <summary>Current redeemable point balance.</summary>
        public int Points { get; set; }

        /// <summary>Cumulative points ever earned (for reporting).</summary>
        public long TotalEarned { get; set; }

        /// <summary>
        /// Lifetime spend on completed (non-cancelled) orders, in VND.
        /// Drives the membership tier (Đồng / Bạc / Vàng).
        /// </summary>
        public decimal TotalSpent { get; set; }

        /// <summary>Unique referral code, e.g. "PT-AB12CD".</summary>
        public string ReferralCode { get; set; }

        /// <summary>Optional date of birth (birthday voucher worker).</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Thời điểm tạo tài khoản (UTC).</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Thời điểm tài khoản được cập nhật lần cuối (UTC).</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
