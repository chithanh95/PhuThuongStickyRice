using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Daily check-in for the 7-day challenge (item 53).
    /// One record per phone per day; streak counts consecutive days ending today.
    /// </summary>
    public class CheckInStreak : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 15;

        public string Phone { get; set; }

        /// <summary>Date part only (UTC).</summary>
        public DateTime CheckInDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
