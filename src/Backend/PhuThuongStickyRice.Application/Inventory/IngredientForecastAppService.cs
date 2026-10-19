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
    public interface IIngredientForecastAppService : IApplicationService
    {
        /// <summary>Admin: forecasts nếp (kg) needed for tomorrow morning.</summary>
        Task<IngredientForecastDto> GetForecastAsync();
    }

    /// <summary>
    /// Item 124: ingredient forecast.
    /// Heuristic (documented, deliberately simple): average portions sold on
    /// the same weekday over the last 4 weeks × NepPerPortionGrams, rounded
    /// up to the nearest 0.5 kg. Cancelled orders are excluded. The cook
    /// should still add a safety margin on holidays — this is an estimate,
    /// not a guarantee.
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

        public virtual async Task<IngredientForecastDto> GetForecastAsync()
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
        private async Task<(int TotalPortions, int WeeksWithData)> LoadPortionsByWeekAsync(DateTime tomorrow, int weekday)
        {
            var lookbackDays = LookbackWeeks * 7;
            var fourWeeksAgo = tomorrow.AddDays(-lookbackDays);

            var queryable = await _orders.GetQueryableAsync();

            // Orders are stored with CreatedAt in UTC — convert to Vietnam
            // time client-side (the history window is small enough).
            var candidates = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= fourWeeksAgo.AddDays(-1)
                        && x.CreatedAt < tomorrow.AddDays(1)
                        && x.Status != OrderStatus.Cancelled)
                    .Select(x => new { x.CreatedAt, Portions = x.Lines.Sum(l => l.Quantity) }));

            var weeks = new int[LookbackWeeks];
            var seen = new int[LookbackWeeks];
            foreach (var order in candidates)
            {
                var local = VietnamTime.ToVietnamTime(order.CreatedAt).Date;
                if (local < tomorrow.AddDays(-lookbackDays) || local >= tomorrow)
                {
                    continue;
                }

                if ((int)local.DayOfWeek != weekday)
                {
                    continue;
                }

                var weekIndex = (int)((tomorrow - local).TotalDays / 7);
                if (weekIndex < 0 || weekIndex >= LookbackWeeks)
                {
                    continue;
                }

                weeks[weekIndex] += order.Portions;
                seen[weekIndex]++;
            }

            return (weeks.Sum(), seen.Count(x => x > 0));
        }

        /// <summary>
        /// Dựng DTO dự báo từ số suất trung bình: kg nếp = trung bình suất ×
        /// gram/suất, làm tròn lên bước 0.5kg.
        /// </summary>
        private IngredientForecastDto BuildForecastDto(
            DateTime tomorrow, int weekday, int gramsPerPortion,
            double averagePortions, int weeksWithData)
        {
            var nepKg = Math.Ceiling(averagePortions * gramsPerPortion / 1000.0 / RoundingStepKg) * RoundingStepKg;
            var weekdayName = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.DayNames[weekday];

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
