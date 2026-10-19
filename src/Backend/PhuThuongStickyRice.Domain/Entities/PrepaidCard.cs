using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Thẻ xôi sáng trả trước (item 164): khách mua gói 20/30 suất,
    /// mỗi sáng đưa mã để nhận 1 suất, admin trừ lượt.
    /// </summary>
    public class PrepaidCard : Entity<Guid>, IAggregateRoot
    {
        public const int CustomerPhoneMaxLength = 30;
        public const int CardCodeMaxLength = 20;

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Mã thẻ duy nhất, khách đưa mã để nhận suất.</summary>
        public string CardCode { get; set; }

        /// <summary>Tổng số suất đã mua.</summary>
        public int TotalSuits { get; set; }

        /// <summary>Số suất đã dùng.</summary>
        public int UsedSuits { get; set; }

        public int RemainingSuits => TotalSuits - UsedSuits;

        public decimal PaidAmount { get; set; }

        public PrepaidCardStatus Status { get; set; } = PrepaidCardStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }
    }

    public enum PrepaidCardStatus
    {
        Active = 0,
        UsedUp = 1,
        Cancelled = 2
    }
}
