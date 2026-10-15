using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Item 155: fine-grained permission grant — which permission keys a
    /// role holds. Admin implicitly holds every key (see PermissionKeys).
    /// Seeded with safe defaults in AdminSeedHostedService; editable at
    /// /admin/phan-quyen.
    /// </summary>
    public class RolePermission : Entity<Guid>, IAggregateRoot
    {
        public const int RoleMaxLength = 50;
        public const int PermissionKeyMaxLength = 100;

        /// <summary>Role name, e.g. "Cashier", "Kitchen".</summary>
        public string Role { get; set; }

        /// <summary>Permission key, e.g. "orders.cancel" (see PermissionKeys).</summary>
        public string PermissionKey { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
