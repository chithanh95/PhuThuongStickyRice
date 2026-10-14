using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Staff work shift (item 125): who works, on which day, which hours.
    /// </summary>
    public class WorkShift : Entity<Guid>, IAggregateRoot
    {
        public const int StaffNameMaxLength = 100;
        public const int NoteMaxLength = 500;

        public string StaffName { get; set; }

        /// <summary>The shift day (date part only).</summary>
        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Note { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
