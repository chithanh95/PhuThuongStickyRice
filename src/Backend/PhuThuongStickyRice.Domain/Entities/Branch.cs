using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Shop branch (chi nhánh). Foundation for multi-branch support.
    /// Products and orders can be linked to a branch; pricing stays
    /// global for now — per-branch pricing is a future item.
    /// </summary>
    public class Branch : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int AddressMaxLength = 500;
        public const int PhoneMaxLength = 30;

        public string Name { get; set; }

        public string Address { get; set; }

        public string Phone { get; set; }

        public bool IsActive { get; set; }
    }
}
