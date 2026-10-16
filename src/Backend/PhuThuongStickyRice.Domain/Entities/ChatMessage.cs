using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// A single message in the landing-page live chat.
    /// Messages are grouped by an anonymous SessionId stored in the
    /// visitor's browser (localStorage), so no login is needed.
    /// </summary>
    public class ChatMessage : Entity<Guid>, IAggregateRoot
    {
        public const int SessionIdMaxLength = 50;
        public const int SenderMaxLength = 10;
        public const int MessageMaxLength = 2000;

        public const string SenderCustomer = "customer";
        public const string SenderAdmin = "admin";

        /// <summary>Anonymous browser session identifier.</summary>
        public string SessionId { get; set; }

        /// <summary>"customer" or "admin".</summary>
        public string Sender { get; set; }

        public string Message { get; set; }

        /// <summary>True once the other side has seen the message.</summary>
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
