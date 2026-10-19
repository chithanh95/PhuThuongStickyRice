using System;
using System.Linq;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Vietnam local time helpers (UTC+7, no daylight saving).
    /// </summary>
    internal static class VietnamTime
    {
        public static DateTime Today()
        {
            return ToVietnamTime(DateTime.UtcNow).Date;
        }

        public static DateTime ToVietnamTime(DateTime utc)
        {
            var utcDateTime = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            foreach (var id in new[] { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" })
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Timezone id not present on this OS — try the next id.
                }
                catch (InvalidTimeZoneException)
                {
                    // Corrupt timezone data — try the next id.
                }
            }

            return utcDateTime.AddHours(7);
        }


        /// <summary>Now in Vietnam time.</summary>
        public static DateTimeOffset Now => DateTimeOffset.UtcNow.AddHours(7);

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

        /// <summary>Digits-only phone; null when invalid.</summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 9 ? digits : null;
        }
    }
}
