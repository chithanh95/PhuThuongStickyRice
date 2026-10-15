using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Monthly reminder subscription for xoi cung (Rằm / mùng 1).
    /// The shop uses this list to remind customers before each occasion.
    /// </summary>
    public class CungReminder : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int PhoneMaxLength = 30;

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>"Ram" (rằm 15 âm) or "Mung1" (mùng 1 âm).</summary>
        public string ReminderType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
