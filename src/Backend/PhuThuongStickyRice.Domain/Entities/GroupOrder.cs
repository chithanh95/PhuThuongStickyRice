using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Group order (đặt nhóm): anyone with the link adds items on
    /// <c>/nhom/{Code}</c>; the creator chốt đơn (closes the group),
    /// which creates a normal COD order from the aggregated lines.
    /// </summary>
    public class GroupOrder : Entity<Guid>, IAggregateRoot
    {
        public const int CodeMaxLength = 20;
        public const int PhoneMaxLength = 30;
        public const int StatusMaxLength = 20;

        public const string StatusOpen = "Open";
        public const string StatusClosed = "Closed";

        public string Code { get; set; }

        public string CreatorPhone { get; set; }

        public string Status { get; set; }

        /// <summary>
        /// JSON array of {ProductId, ProductName, UnitPrice, Quantity, Note, AddedBy}.
        /// Kept as JSON for simplicity — lines are ephemeral.
        /// </summary>
        public string LinesJson { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        /// <summary>Order number created when the group was closed.</summary>
        public string ResultOrderNumber { get; set; }
    }
}
