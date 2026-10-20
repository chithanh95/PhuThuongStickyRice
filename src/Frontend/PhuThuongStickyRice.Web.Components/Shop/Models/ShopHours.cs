using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>
    /// Wave 6a (items 22–23): open-hours helpers shared by the shop hero,
    /// the landing page and the checkout slot picker. All times are Vietnam
    /// local time (UTC+7, no DST), parsed from the ShopSetting "HH:mm"
    /// strings; unparseable values fall back to the documented defaults.
    /// </summary>
    public static class ShopHours
    {
        /// <summary>Giờ mở cửa mặc định khi setting chưa cấu hình hoặc parse lỗi ("HH:mm").</summary>
        public const string DefaultOpenTime = "06:00";

        /// <summary>Giờ đóng cửa mặc định khi setting chưa cấu hình hoặc parse lỗi ("HH:mm").</summary>
        public const string DefaultCloseTime = "10:30";

        /// <summary>Độ dài 1 khung giờ nhận đơn (phút) — hero và trang checkout chia slot theo bước này.</summary>
        public const int SlotMinutes = 30;

        /// <summary>Múi giờ Việt Nam so với UTC (không có giờ mùa hè).</summary>
        public const int VietnamUtcOffsetHours = 7;

        /// <summary>Current Vietnam local time (UTC+7 has no DST).</summary>
        public static TimeSpan NowInVietnam()
        {
            return DateTimeOffset.UtcNow.AddHours(VietnamUtcOffsetHours).TimeOfDay;
        }

        /// <summary>
        /// Parse giờ mở cửa từ setting; giá trị lỗi/thiếu thì dùng
        /// <see cref="DefaultOpenTime"/> để shop vẫn có giờ mở hợp lệ.
        /// </summary>
        public static TimeSpan ParseOpen(string openTime)
        {
            return TryParseHour(openTime, DefaultOpenTime);
        }

        /// <summary>
        /// Parse giờ đóng cửa từ setting; giá trị lỗi/thiếu thì dùng
        /// <see cref="DefaultCloseTime"/> để shop vẫn có giờ đóng hợp lệ.
        /// </summary>
        public static TimeSpan ParseClose(string closeTime)
        {
            return TryParseHour(closeTime, DefaultCloseTime);
        }

        private static TimeSpan TryParseHour(string value, string fallback)
        {
            if (TimeSpan.TryParse(value?.Trim(), out var time)
                && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1))
            {
                return time;
            }

            // Fallbacks are compile-time constants ("06:00"/"10:30") that always
            // parse; the explicit check keeps the analyzer (CA1806) honest.
            return TimeSpan.TryParse(fallback, out var fallbackTime)
                ? fallbackTime
                : TimeSpan.Zero;
        }

        private static string Format(TimeSpan time)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}";
        }

        /// <summary>
        /// Quán có đang mở không, tính theo giờ Việt Nam hiện tại.
        /// Close &lt;= open coi như cấu hình sai → luôn trả false (đang nghỉ).
        /// </summary>
        public static bool IsOpen(string openTime, string closeTime)
        {
            return IsOpen(openTime, closeTime, NowInVietnam());
        }

        /// <summary>Open when now is in [open, close). Overnight ranges are not used.</summary>
        public static bool IsOpen(string openTime, string closeTime, TimeSpan now)
        {
            var open = ParseOpen(openTime);
            var close = ParseClose(closeTime);
            if (close <= open)
            {
                return false;
            }

            return now >= open && now < close;
        }

        /// <summary>
        /// Hero badge text: "🟢 Đang mở cửa (6:00–10:30)" or
        /// "🔴 Quán đang nghỉ — mở lại lúc 6:00".
        /// </summary>
        public static (bool IsOpen, string Text) StatusText(string openTime, string closeTime)
        {
            var open = ParseOpen(openTime);
            var close = ParseClose(closeTime);
            var now = NowInVietnam();
            var openText = $"{open.Hours}:{open.Minutes:D2}";
            var closeText = $"{close.Hours}:{close.Minutes:D2}";

            if (close > open && now >= open && now < close)
            {
                return (true, $"🟢 Đang mở cửa ({openText}–{closeText})");
            }

            return (false, $"🔴 Quán đang nghỉ — mở lại lúc {openText}");
        }

        /// <summary>
        /// Dựng danh sách khung giờ nhận đơn hôm nay theo giờ Việt Nam hiện tại:
        /// các slot đã bắt đầu bị bỏ qua, quán đã đóng thì trả danh sách rỗng.
        /// </summary>
        public static List<string> BuildSlots(string openTime, string closeTime)
        {
            return BuildSlots(openTime, closeTime, NowInVietnam());
        }

        public static List<string> BuildSlots(string openTime, string closeTime, TimeSpan now)
        {
            var slots = new List<string>();
            var open = ParseOpen(openTime);
            var close = ParseClose(closeTime);
            if (close <= open)
            {
                return slots;
            }

            for (var start = open; start.Add(TimeSpan.FromMinutes(SlotMinutes)) <= close; start = start.Add(TimeSpan.FromMinutes(SlotMinutes)))
            {
                if (start <= now)
                {
                    // Only future slots are offered.
                    continue;
                }

                slots.Add($"{Format(start)}–{Format(start.Add(TimeSpan.FromMinutes(SlotMinutes)))}");
            }

            return slots;
        }
    }
}
