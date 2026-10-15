using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// End-of-day inventory count (kiểm kê cuối ngày, item 87): how many
    /// portions of each product are left when the shop closes. One row per
    /// (date, product); saving a date replaces its previous lines.
    /// </summary>
    public class DailyInventory : Entity<Guid>, IAggregateRoot
    {
        public const int NoteMaxLength = 500;
        public const int ProductNameMaxLength = 200;
        public const int CreatedByMaxLength = 200;

        /// <summary>The business day counted (Vietnam date, time part 00:00).</summary>
        public DateTime Date { get; set; }

        public Guid ProductId { get; set; }

        /// <summary>Denormalized product name so history stays readable.</summary>
        public string ProductName { get; set; }

        /// <summary>Portions left at closing time.</summary>
        public int RemainingQty { get; set; }

        public string Note { get; set; }

        /// <summary>Admin/cashier email who saved the count.</summary>
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
