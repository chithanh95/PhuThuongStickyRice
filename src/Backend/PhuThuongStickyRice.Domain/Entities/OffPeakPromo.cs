using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Off-peak promotion (item 48): discount applied when the order is placed
    /// within a configured hour window (e.g. 13h-16h).
    /// </summary>
    public class OffPeakPromo : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Start hour (0-23), inclusive.</summary>
        public int StartHour { get; set; }

        /// <summary>End hour (0-23), exclusive.</summary>
        public int EndHour { get; set; }

        /// <summary>Discount amount in VND applied to qualifying orders.</summary>
        public int DiscountAmount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
