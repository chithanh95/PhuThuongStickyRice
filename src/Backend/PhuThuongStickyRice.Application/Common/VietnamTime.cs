using System;
using System.Linq;
using System.Threading;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Vietnam local time helpers (UTC+7, no daylight saving).
    /// Public để WebAPI (BackgroundWorkers, Controllers) dùng chung
    /// logic "ngày theo giờ VN" với Application.
    /// </summary>
    public static class VietnamTime
    {
        /// <summary>
        /// Độ lệch giờ của Việt Nam so với UTC: +7, không có giờ mùa hè (DST).
        /// </summary>
        public const int UtcOffsetHours = 7;

        /// <summary>
        /// ID timezone Việt Nam theo OS: Linux dùng IANA, Windows dùng
        /// Windows zone ID. Để static readonly để không cấp phát mảng mới
        /// mỗi lần gọi <see cref="ToVietnamTime"/>.
        /// </summary>
        private static readonly string[] VietnamTimeZoneIds =
            { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" };

        /// <summary>
        /// TimeZoneInfo đã resolve, chỉ tìm 1 lần cho cả vòng đời app.
        /// FindSystemTimeZoneById đọc registry/file hệ thống nên không nên
        /// gọi mỗi lần convert (ToVietnamTime được gọi rất nhiều).
        /// </summary>
        private static readonly Lazy<TimeZoneInfo> CachedVietnamTimeZone =
            new(ResolveVietnamTimeZone, LazyThreadSafetyMode.ExecutionAndPublication);

        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            foreach (var id in VietnamTimeZoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                    // ID timezone không có trên OS này — thử id tiếp theo.
                }
                catch (InvalidTimeZoneException)
                {
                    // Dữ liệu timezone hỏng — thử id tiếp theo.
                }
            }

            return null;
        }

        /// <summary>
        /// Ngày hôm nay theo giờ Việt Nam (chỉ phần ngày, không kèm giờ).
        /// </summary>
        public static DateTime Today()
        {
            return ToVietnamTime(DateTime.UtcNow).Date;
        }

        /// <summary>
        /// Đổi thời điểm UTC sang giờ Việt Nam. Input không phải Kind=Utc vẫn
        /// được coi như UTC (quán không có khái niệm múi giờ khác).
        /// </summary>
        /// <param name="utc">Thời điểm UTC cần đổi.</param>
        /// <returns>Thời điểm theo giờ Việt Nam.</returns>
        public static DateTime ToVietnamTime(DateTime utc)
        {
            var utcDateTime = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var vietnamTimeZone = CachedVietnamTimeZone.Value;
            // Không resolve được timezone nào (OS lạ) thì cộng tay +7 như cũ.
            return vietnamTimeZone != null
                ? TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, vietnamTimeZone)
                : utcDateTime.AddHours(UtcOffsetHours);
        }


        /// <summary>Now in Vietnam time.</summary>
        public static DateTimeOffset Now => DateTimeOffset.UtcNow.AddHours(UtcOffsetHours);

        /// <summary>
        /// UTC instant of 00:00 Vietnam time on the given (date-only) day.
        /// </summary>
        public static DateTime DayStartUtc(DateTime date) =>
            date.Date.AddHours(-UtcOffsetHours);

        /// <summary>
        /// UTC instant of 00:00 Vietnam time on the day AFTER the given
        /// (date-only) day — exclusive upper bound of the day.
        /// </summary>
        public static DateTime DayEndExclusiveUtc(DateTime date) =>
            date.Date.AddDays(1).AddHours(-UtcOffsetHours);

        /// <summary>Digits-only phone; null when invalid.</summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            // codequality-team 2026-10-20: dùng PhoneNumber.DigitsOnly thay
            // copy-paste new string(...Where(char.IsDigit)...) — ngữ nghĩa giữ nguyên.
            var digits = PhoneNumber.DigitsOnly(phone);
            return digits.Length >= PhoneNumber.TailDigitCount ? digits : null;
        }
    }
}
