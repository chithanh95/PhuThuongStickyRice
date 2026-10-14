using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Saved delivery address (sổ địa chỉ) keyed by customer phone.
    /// Checkout shows quick-select chips; manageable from the
    /// track-order profile section.
    /// </summary>
    public class AddressBook : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 30;
        public const int LabelMaxLength = 100;
        public const int AddressMaxLength = 500;

        public string Phone { get; set; }

        /// <summary>Label like "Nhà", "Công ty".</summary>
        public string Label { get; set; }

        public string Address { get; set; }
    }
}
