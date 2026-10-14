using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// COD end-of-shift reconciliation (item 129): the expected COD cash is
    /// auto-summed from the day's Completed COD orders; the cashier enters
    /// the actual cash counted and a discrepancy is flagged.
    /// Amounts in VND.
    /// </summary>
    public class ShiftClose : Entity<Guid>, IAggregateRoot
    {
        public const int NoteMaxLength = 500;

        /// <summary>The business day closed (Vietnam date, time part 00:00).</summary>
        public DateTime Date { get; set; }

        /// <summary>Auto-summed from Completed COD orders of the day.</summary>
        public decimal ExpectedCod { get; set; }

        /// <summary>Cash actually counted by the cashier.</summary>
        public decimal ActualCod { get; set; }

        public string Note { get; set; }

        /// <summary>Admin/cashier email who closed the shift.</summary>
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
