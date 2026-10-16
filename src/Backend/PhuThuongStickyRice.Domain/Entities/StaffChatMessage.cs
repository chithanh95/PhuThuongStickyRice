using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// A single message in the internal staff team chat (shift communication).
    /// Simple broadcast channel: every staff member sees all messages.
    /// </summary>
    public class StaffChatMessage : Entity<Guid>, IAggregateRoot
    {
        public const int SenderNameMaxLength = 256;
        public const int MessageMaxLength = 2000;

        /// <summary>Id of the staff user who sent the message.</summary>
        public Guid SenderId { get; set; }

        /// <summary>Display name of the sender (denormalized for fast reads).</summary>
        public string SenderName { get; set; }

        public string Message { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
