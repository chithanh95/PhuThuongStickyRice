using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Loyalty.DTOs
{
    /// <summary>
    /// Tóm tắt tài khoản loyalty của 1 SĐT (điểm, hạng, tổng chi tiêu) — dùng
    /// cho trang "hồ sơ" của khách và màn hình admin.
    /// </summary>
    public class LoyaltyAccountDto
    {
        public string Phone { get; set; }

        /// <summary>
        /// Điểm khả dụng để đổi (số dư). Trừ khi đổi điểm, cộng khi tích điểm —
        /// không bao giờ âm.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Tổng điểm đã từng kiếm được trong lịch sử (chỉ tăng, không giảm khi
        /// đổi điểm) — dùng để đối soát, khác với <see cref="Points"/> là số dư.
        /// </summary>
        public long TotalEarned { get; set; }

        /// <summary>Tổng tiền đã chi tiêu (quyết định hạng Đồng/Bạc/Vàng).</summary>
        public decimal TotalSpent { get; set; }

        /// <summary>Mã giới thiệu của chính tài khoản này (để giới thiệu người khác).</summary>
        public string ReferralCode { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>Đồng / Bạc / Vàng, computed from TotalSpent.</summary>
        public string Tier { get; set; }

        /// <summary>Tier discount percent applied at checkout.</summary>
        public decimal TierDiscountPercent { get; set; }
    }

    /// <summary>Admin cộng/trừ điểm thủ công cho 1 SĐT (kèm lý do để đối soát).</summary>
    public class AdjustPointsInput
    {
        /// <summary>Positive to add, negative to subtract.</summary>
        [Range(-1000000, 1000000)]
        public int PointsDelta { get; set; }

        /// <summary>Lý do điều chỉnh (hiển thị trong lịch sử, tối đa 200 ký tự).</summary>
        [StringLength(200)]
        public string Reason { get; set; }
    }

    /// <summary>Khách tự cập nhật ngày sinh của mình (dùng để tặng quà sinh nhật).</summary>
    public class UpdateBirthdayInput
    {
        public DateTime? DateOfBirth { get; set; }
    }
}
