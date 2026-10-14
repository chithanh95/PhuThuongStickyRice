using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Free-text CRM note attached to a customer phone number.
    /// </summary>
    public class CustomerNote : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int NoteMaxLength = 2000;

        public string Phone { get; set; }

        public string Note { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
