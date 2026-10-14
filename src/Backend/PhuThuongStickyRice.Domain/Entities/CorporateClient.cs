using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Corporate / weekly customer (item 122): a company that orders the same
    /// items every week on a fixed weekday. The admin converts the due
    /// reminder into a real order manually — no auto-charge.
    /// </summary>
    public class CorporateClient : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int PhoneMaxLength = 30;
        public const int AddressMaxLength = 500;

        public string CompanyName { get; set; }

        public string ContactName { get; set; }

        public string Phone { get; set; }

        public string Address { get; set; }

        /// <summary>0 = Sunday … 6 = Saturday (matches <see cref="DayOfWeek"/>).</summary>
        public int Weekday { get; set; }

        /// <summary>
        /// Standing weekly order, JSON array of {productId, productName, qty}.
        /// </summary>
        public string ItemsJson { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
