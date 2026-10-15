using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Shop holiday schedule (lịch nghỉ lễ/Tết). When the Vietnam calendar
    /// date matches <see cref="HolidayDate"/>, the storefront automatically
    /// switches to "paused" mode with the holiday reason — reusing the
    /// existing AcceptingOrders/PausedReason mechanism.
    /// </summary>
    public class HolidaySchedule : Entity<Guid>, IAggregateRoot
    {
        public const int ReasonMaxLength = 200;

        /// <summary>
        /// The holiday date (Vietnam calendar date). Only the date part is
        /// significant; stored as midnight.
        /// </summary>
        public DateTime HolidayDate { get; set; }

        /// <summary>Reason shown to customers, e.g. "Nghỉ Tết Nguyên đán".</summary>
        public string Reason { get; set; }

        /// <summary>
        /// True = closed the whole day (auto-pauses ordering).
        /// False = partial day off (informational only, shown on the landing).
        /// </summary>
        public bool IsFullDay { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
