using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Daily check-in (item 121): one record per phone per day. Awards
    /// loyalty points (amount from <c>ShopSetting.CheckInPoints</c>).
    /// </summary>
    public class CheckIn : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;

        /// <summary>Normalized phone (digits only where possible).</summary>
        public string Phone { get; set; }

        /// <summary>The check-in day (Vietnam date, time part = 00:00).</summary>
        public DateTime Date { get; set; }

        /// <summary>Points awarded for this check-in.</summary>
        public int PointsAwarded { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
