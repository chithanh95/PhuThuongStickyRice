using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Inventory;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Inventory
{
    /// <summary>
    /// Hợp đồng dự báo nguyên liệu cho admin: lượng nếp (kg) cần chuẩn bị
    /// cho sáng mai, ước tính từ lịch sử cùng thứ trong tuần.
    /// </summary>
    public interface IIngredientForecastAppService : IApplicationService
    {
        /// <summary>
        /// Admin: dự báo lượng nếp (kg) cần cho sáng mai, từ số suất trung
        /// bình bán được vào cùng thứ trong 4 tuần gần nhất.
        /// </summary>
        /// <returns>Lượng nếp gợi ý (làm tròn lên 0.5kg) kèm giải thích cách tính.</returns>
        Task<IngredientForecastDto> GetIngredientForecastAsync();
    }

    /// <summary>
    /// Item 124: dự báo nguyên liệu.
    /// Heuristic (được ghi lại, cố ý đơn giản): trung bình số suất bán được
    /// vào cùng thứ trong 4 tuần gần nhất × số gram nếp/suất, làm tròn lên
    /// bước 0.5 kg. Đơn bị hủy bị loại. Người nấu vẫn nên cộng thêm dự phòng
    /// vào ngày lễ — đây chỉ là ước tính, không phải cam kết.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class IngredientForecastAppService : ApplicationService, IIngredientForecastAppService
    {
        // Số tuần lịch sử dùng để tính trung bình cùng ngày trong tuần.
        private const int LookbackWeeks = 4;

        // Gram nếp/suất dùng khi shop chưa cài đặt (giá trị ước lượng hợp lý).
        private const int FallbackNepPerPortionGrams = 150;

        // Làm tròn lượng nếp lên bước 0.5 kg.
        private const double RoundingStepKg = 0.5;

        // Quy đổi gram → kg trong công thức dự báo (suất × gram/suất → kg).
        private const double GramsPerKilogram = 1000.0;

        private readonly IRepository<Order, Guid> _orders;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public IngredientForecastAppService(
            IRepository<Order, Guid> orders,
            IShopSettingAppService shopSettings,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _shopSettings = shopSettings;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Dự báo lượng nếp (kg) cần cho sáng mai: lấy gram nếp/suất từ cài đặt
        /// shop (chưa cài thì dùng giá trị ước lượng), nhân với số suất trung
        /// bình của cùng thứ trong 4 tuần gần nhất.
        /// </summary>
        /// <returns>Lượng nếp gợi ý (làm tròn lên 0.5kg) kèm giải thích cách tính.</returns>
        public virtual async Task<IngredientForecastDto> GetIngredientForecastAsync()
        {
            var settings = await _shopSettings.GetAsync();
            var gramsPerPortion = settings.NepPerPortionGrams > 0 ? settings.NepPerPortionGrams : FallbackNepPerPortionGrams;

            var tomorrow = VietnamTime.Today().AddDays(1);
            var weekday = (int)tomorrow.DayOfWeek;

            var (totalPortions, weeksWithData) = await LoadPortionsByWeekAsync(tomorrow, weekday);

            var averagePortions = weeksWithData > 0
                ? (double)totalPortions / weeksWithData
                : 0;

            return BuildForecastDto(tomorrow, weekday, gramsPerPortion, averagePortions, weeksWithData);
        }

        /// <summary>
        /// Gom tổng số suất bán được của cùng thứ trong tuần qua các tuần
        /// lịch sử. CreatedAt lưu UTC nên convert sang giờ VN khi lọc.
        /// </summary>
        /// <remarks>
        /// perf-hunt 2026-10-19 (run 15:25): gộp số suất theo ngày VN ngay trên
        /// SQL (GROUP BY ngày của DATEADD(hour, 7, CreatedAt) — VN không DST nên
        /// dịch được sang SQL) thay vì nạp toàn bộ đơn trong cửa sổ lịch sử vào
        /// memory rồi gộp bằng C#. Về memory chỉ còn tối đa ~lookbackDays dòng
        /// (1 dòng/ngày có đơn); vòng lọc thứ-trong-tuần và chia chỉ số tuần
        /// giữ nguyên ở C# nên tổng suất và số tuần có dữ liệu giống hệt bản cũ.
        /// </remarks>
        private async Task<(int TotalPortions, int WeeksWithData)> LoadPortionsByWeekAsync(DateTime tomorrow, int weekday)
        {
            var lookbackDays = LookbackWeeks * 7;
            var vnFrom = tomorrow.AddDays(-lookbackDays);

            var queryable = await _orders.GetQueryableAsync();

            // Biên UTC bao phủ đúng các đơn có ngày VN trong [vnFrom, tomorrow):
            // ngày VN d tương ứng UTC [d-7h, d+1-7h).
            var perOrder = queryable
                .Where(x => x.CreatedAt >= vnFrom.AddHours(-VietnamTime.UtcOffsetHours)
                    && x.CreatedAt < tomorrow.AddHours(-VietnamTime.UtcOffsetHours)
                    && x.Status != OrderStatus.Cancelled)
                .Select(x => new
                {
                    Day = x.CreatedAt.AddHours(VietnamTime.UtcOffsetHours).Date,
                    Portions = x.Lines.Sum(l => (long)l.Quantity)
                });

            var dayRows = await _asyncExecuter.ToListAsync(
                perOrder
                    .Where(x => x.Day >= vnFrom && x.Day < tomorrow)
                    .GroupBy(x => x.Day)
                    .Select(g => new { Day = g.Key, Portions = g.Sum(x => x.Portions) }));

            var weeks = new long[LookbackWeeks];
            var seen = new bool[LookbackWeeks];
            foreach (var row in dayRows)
            {
                // Chỉ lấy các ngày cùng thứ với ngày mai (ví dụ Thứ 7) — pattern
                // bán hàng phụ thuộc mạnh vào thứ trong tuần.
                if ((int)row.Day.DayOfWeek != weekday)
                {
                    continue;
                }

                // Chỉ số tuần lịch sử: 0 = tuần vừa rồi, 3 = 4 tuần trước.
                var weekIndex = (int)((tomorrow - row.Day).TotalDays / 7);
                if (weekIndex < 0 || weekIndex >= LookbackWeeks)
                {
                    continue;
                }

                weeks[weekIndex] += row.Portions;
                seen[weekIndex] = true;
            }

            return ((int)weeks.Sum(), seen.Count(x => x));
        }

        /// <summary>
        /// Dựng DTO dự báo từ số suất trung bình: kg nếp = trung bình suất ×
        /// gram/suất, làm tròn lên bước 0.5kg.
        /// </summary>
        private static IngredientForecastDto BuildForecastDto(
            DateTime tomorrow, int weekday, int gramsPerPortion,
            double averagePortions, int weeksWithData)
        {
            var nepKg = Math.Ceiling(averagePortions * gramsPerPortion / GramsPerKilogram / RoundingStepKg) * RoundingStepKg;
            var weekdayName = VietnameseCulture.Value.DateTimeFormat.DayNames[weekday];

            return new IngredientForecastDto
            {
                Date = tomorrow,
                WeekdayName = weekdayName,
                AveragePortions = Math.Round(averagePortions, 1),
                NepKg = nepKg,
                NepPerPortionGrams = gramsPerPortion,
                Heuristic = $"Trung bình {Math.Round(averagePortions, 1)} suất vào {weekdayName.ToLowerInvariant()} " +
                    $"trong {weeksWithData}/{LookbackWeeks} tuần qua × {gramsPerPortion}g/suất, làm tròn lên 0.5kg. " +
                    "Chỉ là ước tính — ngày lễ/tiệc lớn cần cộng thêm dự phòng."
            };
        }
    }
}
