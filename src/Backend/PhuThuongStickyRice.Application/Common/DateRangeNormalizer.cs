using System;

namespace PhuThuongStickyRice.Application.Common
{
    /// <summary>
    /// Chuẩn hóa khoảng ngày dùng chung cho các màn báo cáo: biên "nửa mở"
    /// [From, To) — To là ngày kết thúc + 1 ngày (mốc độc quyền) để query
    /// <c>CreatedAt &lt; To</c> bao trọn cả ngày kết thúc; mặc định lấy
    /// 7 ngày gần nhất. Gom bản copy-paste trước đây nằm ở DashboardAppService
    /// và ProfitReportAppService về 1 chỗ để đổi 1 lần là áp dụng khắp nơi.
    /// </summary>
    // [codequality-naming 2026-10-19] Đổi tên DateRangeHelper → DateRangeNormalizer:
    // tên cũ "Helper" chung chung, tên mới mô tả đúng chức năng duy nhất của class.
    public static class DateRangeNormalizer
    {
        // [codequality-smell 2026-10-20] Hằng số có tên thay magic number "7"
        // (dùng ở 2 nhánh): mặc định báo cáo lấy 7 ngày gần nhất.
        private const int DefaultRangeDays = 7;

        /// <summary>
        /// Chuẩn hóa khoảng ngày: from/to null thì dùng mặc định (7 ngày gần
        /// nhất, kết thúc hôm nay); ToDate được cộng 1 ngày để bao trọn ngày
        /// kết thúc. Nếu from &gt; to thì lùi from về to − 7 ngày thay vì trả
        /// lỗi — để date-picker lỡ chọn ngược vẫn ra báo cáo.
        /// </summary>
        /// <param name="from">Ngày bắt đầu (chỉ lấy phần ngày).</param>
        /// <param name="to">Ngày kết thúc (chỉ lấy phần ngày).</param>
        /// <returns>(From, To) theo biên nửa mở [From, To).</returns>
        public static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
        {
            var toDate = (to?.Date ?? DateTime.UtcNow.Date).AddDays(1);
            var fromDate = from?.Date ?? toDate.AddDays(-DefaultRangeDays);
            if (fromDate > toDate)
            {
                fromDate = toDate.AddDays(-DefaultRangeDays);
            }

            return (fromDate, toDate);
        }
    }
}
