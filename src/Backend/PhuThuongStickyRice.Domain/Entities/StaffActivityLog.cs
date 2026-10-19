using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Item 156: staff activity log — who did what and when. Written by
    /// StaffActivityLogger from the application services; never throws
    /// into the business flow. Viewed at /admin/nhat-ky (Admin only).
    /// </summary>
    public class StaffActivityLog : Entity<Guid>, IAggregateRoot
    {
        public const int UserNameMaxLength = 200;
        public const int ActionMaxLength = 100;
        public const int DetailMaxLength = 1000;

        public Guid? UserId { get; set; }

        public string UserName { get; set; }

        /// <summary>Machine key, e.g. "order.cancel" (see StaffActivityActions).</summary>
        public string Action { get; set; }

        /// <summary>Human-readable detail, e.g. order number.</summary>
        public string Detail { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
