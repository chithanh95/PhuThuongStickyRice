using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Days-of-week bit flags for <see cref="Promotion.DaysOfWeekFlags"/>.
    /// Monday = 1, Tuesday = 2, Wednesday = 4, ..., Sunday = 64.
    /// </summary>
    [Flags]
    public enum PromotionDay
    {
        None = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 4,
        Thursday = 8,
        Friday = 16,
        Saturday = 32,
        Sunday = 64,
        All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
    }

    /// <summary>
    /// "Khuyến mãi giờ vàng": time-window promotion auto-applied at checkout.
    /// Times are local Vietnam time (UTC+7) as TimeSpan-of-day.
    /// </summary>
    public class Promotion : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;

        public string Name { get; set; }

        /// <summary>Percent off the subtotal, e.g. 10 = 10%.</summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>Local start time (Vietnam), e.g. 14:00.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Local end time (Vietnam). May cross midnight (EndTime &lt; StartTime).</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Bit flags of <see cref="PromotionDay"/>.</summary>
        public int DaysOfWeekFlags { get; set; } = (int)PromotionDay.All;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// True when the given Vietnam-local time falls inside this
        /// promotion's window (day flags + start/end, supports overnight).
        /// </summary>
        public bool IsActiveAt(DateTime vnNow)
        {
            if (!IsActive)
            {
                return false;
            }

            var dayFlag = vnNow.DayOfWeek switch
            {
                DayOfWeek.Monday => PromotionDay.Monday,
                DayOfWeek.Tuesday => PromotionDay.Tuesday,
                DayOfWeek.Wednesday => PromotionDay.Wednesday,
                DayOfWeek.Thursday => PromotionDay.Thursday,
                DayOfWeek.Friday => PromotionDay.Friday,
                DayOfWeek.Saturday => PromotionDay.Saturday,
                _ => PromotionDay.Sunday
            };

            if ((DaysOfWeekFlags & (int)dayFlag) == 0)
            {
                return false;
            }

            var time = vnNow.TimeOfDay;
            if (EndTime >= StartTime)
            {
                return time >= StartTime && time < EndTime;
            }

            // Overnight window, e.g. 22:00–02:00.
            return time >= StartTime || time < EndTime;
        }
    }
}
