using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Wave 6c (item 34): audit trail of order status transitions. Written
    /// server-side on every status change (admin actions, KDS taps, customer
    /// cancels, auto-cancel worker). Shown on the admin order detail page
    /// and on the /bep kitchen cards.
    /// </summary>
    public class OrderStatusHistory : Entity<Guid>
    {
        public const int ByUserMaxLength = 200;

        /// <summary>Id đơn hàng bị đổi trạng thái.</summary>
        public Guid OrderId { get; set; }

        /// <summary>Navigation tới đơn hàng.</summary>
        public Order Order { get; set; }

        /// <summary>Trạng thái trước khi đổi.</summary>
        public OrderStatus FromStatus { get; set; }

        /// <summary>Trạng thái sau khi đổi.</summary>
        public OrderStatus ToStatus { get; set; }

        /// <summary>UTC timestamp of the transition.</summary>
        public DateTime At { get; set; }

        /// <summary>Who made the change (username, or "system" for workers).</summary>
        public string ByUser { get; set; }
    }
}
