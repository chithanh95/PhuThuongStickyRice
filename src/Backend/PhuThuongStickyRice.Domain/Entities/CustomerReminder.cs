using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Reminder sent to an inactive customer ("lâu chưa quay lại").
    /// The actual delivery is via Zalo OA (<see cref="Application.Zalo.IZaloNotificationService"/>),
    /// which is a stub until the Zalo OA credentials are configured — this
    /// entity is the send history, honest about what was (not) delivered.
    /// </summary>
    public class CustomerReminder : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int NameMaxLength = 200;
        public const int MessageMaxLength = 1000;
        public const int SentViaMaxLength = 50;
        public const int CreatedByMaxLength = 100;

        public string CustomerPhone { get; set; }

        public string CustomerName { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// Delivery channel actually used, e.g. "ZaloOA" (stub until configured).
        /// </summary>
        public string SentVia { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
