using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Delivery shipper. Orders can be assigned to an active shipper when
    /// they reach the Delivering status.
    /// </summary>
    public class Shipper : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 100;
        public const int PhoneMaxLength = 30;

        public string Name { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
