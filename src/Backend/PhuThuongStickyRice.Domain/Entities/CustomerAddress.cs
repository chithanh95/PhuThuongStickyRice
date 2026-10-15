using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Saved address of a registered customer (sổ địa chỉ), linked to the
    /// Identity user. Only the owner can view/modify their addresses.
    /// </summary>
    public class CustomerAddress : Entity<Guid>, IAggregateRoot
    {
        public const int LabelMaxLength = 100;
        public const int ReceiverNameMaxLength = 200;
        public const int PhoneMaxLength = 30;
        public const int AddressMaxLength = 500;
        public const int NoteMaxLength = 500;

        public Guid UserId { get; set; }

        /// <summary>Label like "Nhà", "Công ty".</summary>
        public string Label { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string AddressDetail { get; set; }

        public string Note { get; set; }

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
