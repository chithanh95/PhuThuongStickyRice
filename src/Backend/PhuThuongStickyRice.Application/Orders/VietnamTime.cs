using System;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Vietnam local time helpers (UTC+7, no daylight saving).
    /// </summary>
    internal static class VietnamTime
    {
        /// <summary>Now in Vietnam time.</summary>
        public static DateTimeOffset Now => DateTimeOffset.UtcNow.AddHours(7);

        /// <summary>Today's date in Vietnam time.</summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// UTC instant of 00:00 Vietnam time on the given (date-only) day.
        /// </summary>
        public static DateTime DayStartUtc(DateTime date) =>
            date.Date.AddHours(-7);

        /// <summary>
        /// UTC instant of 00:00 Vietnam time on the day AFTER the given
        /// (date-only) day — exclusive upper bound of the day.
        /// </summary>
        public static DateTime DayEndExclusiveUtc(DateTime date) =>
            date.Date.AddDays(1).AddHours(-7);
    }
}
