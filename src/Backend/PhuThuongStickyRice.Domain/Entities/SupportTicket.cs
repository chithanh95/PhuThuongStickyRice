using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    public enum SupportTicketStatus
    {
        New = 0,
        InProgress = 1,
        Done = 2
    }

    /// <summary>
    /// Customer support ticket from the public /ho-tro form.
    /// </summary>
    public class SupportTicket : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int PhoneMaxLength = 30;
        public const int OrderNumberMaxLength = 30;
        public const int MessageMaxLength = 2000;
        public const int ReplyNoteMaxLength = 2000;

        public string Name { get; set; }

        public string Phone { get; set; }

        /// <summary>Optional related order number.</summary>
        public string OrderNumber { get; set; }

        public string Message { get; set; }

        public SupportTicketStatus Status { get; set; }

        /// <summary>Admin reply note (internal).</summary>
        public string ReplyNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RepliedAt { get; set; }
    }
}
