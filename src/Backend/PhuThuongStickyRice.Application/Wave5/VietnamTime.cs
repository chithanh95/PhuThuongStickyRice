using System;
using System.Linq;

namespace PhuThuongStickyRice.Application.Wave5
{
    /// <summary>
    /// Wave 5 date helper: business days (check-in, forecast, shift close)
    /// are measured in Vietnam time (UTC+7), not UTC.
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
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return utcDateTime.AddHours(7);
        }

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
