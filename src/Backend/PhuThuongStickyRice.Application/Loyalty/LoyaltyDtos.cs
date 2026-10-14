using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Loyalty.DTOs
{
    public class LoyaltyAccountDto
    {
        public string Phone { get; set; }

        public int Points { get; set; }

        public long TotalEarned { get; set; }

        public decimal TotalSpent { get; set; }

        public string ReferralCode { get; set; }

        public DateTime? DateOfBirth { get; set; }

        /// <summary>Đồng / Bạc / Vàng, computed from TotalSpent.</summary>
        public string Tier { get; set; }

        /// <summary>Tier discount percent applied at checkout.</summary>
        public decimal TierDiscountPercent { get; set; }
    }

    public class AdjustPointsInput
    {
        /// <summary>Positive to add, negative to subtract.</summary>
        [Range(-1000000, 1000000)]
        public int PointsDelta { get; set; }

        [StringLength(200)]
        public string Reason { get; set; }
    }

    public class UpdateBirthdayInput
    {
        public DateTime? DateOfBirth { get; set; }
    }
}
