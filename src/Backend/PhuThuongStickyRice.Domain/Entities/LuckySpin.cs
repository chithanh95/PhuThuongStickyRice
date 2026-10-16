using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// One spin of the landing-page lucky wheel (one spin per phone per day).
    /// The winner gets a discount voucher code they can use at checkout.
    /// </summary>
    public class LuckySpin : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 15;
        public const int PrizeLabelMaxLength = 50;
        public const int VoucherCodeMaxLength = 20;

        public string Phone { get; set; }

        /// <summary>Date part only (UTC); enforces one spin per phone per day.</summary>
        public DateTime SpinDate { get; set; }

        /// <summary>Display label, e.g. "10k".</summary>
        public string PrizeLabel { get; set; }

        /// <summary>Prize value in VND, e.g. 10000.</summary>
        public int PrizeValue { get; set; }

        /// <summary>Unique voucher code, e.g. "XOIABC123".</summary>
        public string VoucherCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
