using System;

namespace PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods
{
    public static class DateTimeExtensions
    {
        public static DateTime FirstDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime LastDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);
        }
    }
}
