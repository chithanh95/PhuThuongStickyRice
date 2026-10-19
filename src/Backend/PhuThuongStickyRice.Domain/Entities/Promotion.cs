using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Bit flags ngày trong tuần cho <see cref="Promotion.DaysOfWeekFlags"/>.
    /// Thứ 2 = 1, Thứ 3 = 2, Thứ 4 = 4, ..., Chủ nhật = 64.
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
    /// "Khuyến mãi giờ vàng": khuyến mãi theo khung giờ tự áp ở checkout.
    /// Giờ dùng là giờ địa phương Việt Nam (UTC+7), dạng TimeSpan trong ngày.
    /// </summary>
    public class Promotion : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;

        public string Name { get; set; }

        /// <summary>Phần trăm giảm trên tổng tiền hàng, vd 10 = giảm 10%.</summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>Giờ bắt đầu (giờ địa phương Việt Nam), vd 14:00.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Giờ kết thúc (giờ địa phương Việt Nam). Có thể qua đêm (EndTime &lt; StartTime).</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Bit flags của <see cref="PromotionDay"/> — những ngày được áp dụng.</summary>
        public int DaysOfWeekFlags { get; set; } = (int)PromotionDay.All;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// True khi thời điểm cho trước (giờ Việt Nam) nằm trong khung giờ
        /// khuyến mãi (ngày trong tuần + giờ bắt đầu/kết thúc, hỗ trợ qua đêm).
        /// </summary>
        /// <param name="vnNow">Thời điểm kiểm tra (giờ Việt Nam).</param>
        /// <returns>True nếu khuyến mãi đang trong khung áp dụng.</returns>
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

            // Khung giờ qua đêm, vd 22:00–02:00.
            return time >= StartTime || time < EndTime;
        }
    }
}
