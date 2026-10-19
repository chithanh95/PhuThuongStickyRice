using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Hợp đồng dự báo doanh thu cho admin: doanh thu trung bình các ngày đã
    /// qua, pattern theo thứ trong tuần, và dự báo 7/30 ngày tới (heuristic,
    /// chỉ là ước tính).
    /// </summary>
    public interface IOrderForecastAppService : IApplicationService
    {
        /// <summary>
        /// Admin: dự báo doanh thu dựa trên trung bình ngày lịch sử (đơn đã
        /// hủy bị loại; ngày không có đơn tính là 0).
        /// </summary>
        /// <returns>Số liệu lịch sử, pattern theo thứ và dự báo 7/30 ngày tới.</returns>
        Task<RevenueForecastDto> GetRevenueForecastAsync();
    }

    /// <summary>
    /// Dự báo doanh thu. Heuristic, cố ý đơn giản và được ghi lại:
    /// <list type="bullet">
    /// <item>Nhìn lại 56 ngày có dữ liệu đầy đủ gần nhất (8 tuần trọn) của các đơn không bị hủy.</item>
    /// <item>Tính trung bình theo từng thứ trong tuần (Thứ 2..Chủ nhật) — ví dụ Thứ 7 thường gấp ~2 lần ngày thường.</item>
    /// <item>Dự báo mỗi ngày tương lai từ trung bình của đúng thứ đó; khi một thứ có ít hơn 2 điểm dữ liệu thì fallback sang trung bình ngày 30 ngày gần nhất.</item>
    /// <item>Trung bình trượt (7d/30d) chỉ tính trên các ngày đã hoàn tất — hôm nay chưa bán xong nên bị loại.</item>
    /// </list>
    /// Ngày không có doanh thu tính là 0 (quán mở cửa hàng ngày); ghi chú phương
    /// pháp nằm ở DTO. Đây là ước tính, không phải cam kết.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class OrderForecastAppService : ApplicationService, IOrderForecastAppService
    {
        private const int PatternDays = 56; // 8 tuần
        private const int MinWeekdayOccurrences = 2;

        /// <summary>Số ngày lịch sử vẽ biểu đồ.</summary>
        private const int HistoryDays = 30;

        /// <summary>Chân trời dự báo ngắn (ngày).</summary>
        private const int Forecast7d = 7;

        /// <summary>Chân trời dự báo dài (ngày).</summary>
        private const int Forecast30d = 30;

        /// <summary>Giá trị đơn trung bình giả định khi chưa có dữ liệu lịch sử.</summary>
        private const decimal FallbackAvgOrderValueVnd = 30000m;

        /// <summary>Số suất trung bình mỗi đơn giả định khi chưa có dữ liệu.</summary>
        private const double FallbackPortionsPerOrder = 1.5;

        /// <summary>Thứ tự T2..CN cho UI tiếng Việt.</summary>
        private static readonly DayOfWeek[] OrderedWeekdays =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public OrderForecastAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Dự báo doanh thu dựa trên trung bình ngày lịch sử (đơn đã hủy bị
        /// loại; ngày không có đơn tính là 0). Bao gồm chuỗi lịch sử 30 ngày,
        /// pattern theo thứ, và dự báo 7/30 ngày tới kèm ghi chú phương pháp.
        /// </summary>
        /// <returns>Số liệu lịch sử, pattern theo thứ và dự báo.</returns>
        public virtual async Task<RevenueForecastDto> GetRevenueForecastAsync()
        {
            var today = VietnamTime.Today();
            var patternStart = today.AddDays(-PatternDays);
            var perDay = await LoadDailyAggregatesAsync(today, patternStart);

            var vi = CultureInfo.GetCultureInfo("vi-VN");
            var result = new RevenueForecastDto
            {
                GeneratedAt = DateTime.UtcNow,
                HistoryDays = PatternDays,
                DaysWithOrders = perDay.Count
            };

            // Trung bình trượt trên các ngày đã hoàn tất (hôm nay chưa bán xong nên loại).
            result.AvgDailyRevenue7d = SumRevenue(perDay, today.AddDays(-7), today) / 7m;
            result.AvgDailyRevenue30d = SumRevenue(perDay, today.AddDays(-30), today) / 30m;

            BuildHistorySeries(result, perDay, today);
            BuildWeekdayPattern(result, perDay, patternStart, today, vi);

            // Số suất trung bình mỗi đơn — dùng cho đường fallback khi thiếu dữ liệu thứ.
            var totalPortions = perDay.Values.Sum(x => (double)x.Portions);
            var totalOrders = perDay.Values.Sum(x => (double)x.Orders);
            var portionsPerOrder = totalOrders > 0 ? totalPortions / totalOrders : FallbackPortionsPerOrder;
            BuildForecasts(result, perDay, today, vi, totalOrders, portionsPerOrder);

            result.MethodNote =
                $"Trung bình trượt trên {PatternDays} ngày đã qua (đơn đã hủy bị loại, " +
                $"ngày không có đơn tính là 0). Dự báo mỗi ngày = trung bình của đúng " +
                $"thứ đó trong lịch sử (cần ≥ {MinWeekdayOccurrences} lần xuất hiện), " +
                $"nếu thiếu dữ liệu thì dùng trung bình ngày 30 ngày gần nhất " +
                $"({Math.Round(result.AvgDailyRevenue30d, 0):N0}₫/ngày). Số suất gợi ý làm tròn lên.";

            result.Disclaimer =
                "Dự báo dựa trên trung bình lịch sử, không chính xác tuyệt đối. " +
                "Ngày lễ, tiệc lớn, thời tiết xấu hoặc món mới có thể làm thực tế " +
                "lệch nhiều — hãy cộng thêm dự phòng theo kinh nghiệm của quán.";

            return result;
        }

        /// <summary>Tổng hợp theo từng ngày (giờ VN).</summary>
        private sealed record DayAggregate(decimal Revenue, long Orders, long Portions);

        /// <summary>
        /// Tổng hợp doanh thu/số đơn/số suất theo từng ngày (giờ VN) trong
        /// [patternStart, today). Ngày không có đơn tính là 0 (quán mở cửa hàng ngày).
        /// </summary>
        /// <remarks>
        /// perf-hunt 2026-10-19 (run 15:25): gộp theo ngày VN ngay trên SQL
        /// (GROUP BY ngày của DATEADD(hour, 7, CreatedAt) — VN không DST nên
        /// dịch được sang SQL, cùng pattern với OrderHourlyReport) thay vì nạp
        /// toàn bộ đơn trong 57 ngày vào memory rồi gộp bằng C#. Tập ngày VN
        /// và bộ ba (doanh thu, số đơn, số suất) của mỗi ngày giống hệt bản cũ:
        /// bản cũ cũng chỉ giữ các đơn có ngày VN trong [patternStart, today)
        /// (cửa sổ fetch UTC rộng hơn chỉ để trừ hao múi giờ, các đơn thừa bị
        /// lọc bỏ ở C#). Số ngày có đơn (DaysWithOrders) không đổi vì GROUP BY
        /// chỉ sinh dòng cho ngày có đơn.
        /// </remarks>
        private async Task<Dictionary<DateTime, DayAggregate>> LoadDailyAggregatesAsync(
            DateTime today, DateTime patternStart)
        {
            // Biên UTC bao phủ đúng các đơn có ngày VN trong [patternStart, today):
            // ngày VN d tương ứng UTC [d-7h, d+1-7h).
            var utcFrom = patternStart.AddHours(-VietnamTime.UtcOffsetHours);
            var utcTo = today.AddHours(-VietnamTime.UtcOffsetHours);

            var queryable = await _orders.GetQueryableAsync();
            var perOrder = queryable
                .Where(x => x.CreatedAt >= utcFrom
                    && x.CreatedAt < utcTo
                    && x.Status != OrderStatus.Cancelled)
                .Select(x => new
                {
                    Day = x.CreatedAt.AddHours(VietnamTime.UtcOffsetHours).Date,
                    Revenue = x.TotalAmount.Amount,
                    Portions = x.Lines.Sum(l => (long)l.Quantity)
                });

            var rows = await _asyncExecuter.ToListAsync(
                perOrder
                    .Where(x => x.Day >= patternStart && x.Day < today)
                    .GroupBy(x => x.Day)
                    .Select(g => new
                    {
                        Day = g.Key,
                        Revenue = g.Sum(x => x.Revenue),
                        Orders = g.LongCount(),
                        Portions = g.Sum(x => x.Portions)
                    }));

            return rows.ToDictionary(
                x => x.Day,
                x => new DayAggregate(x.Revenue, x.Orders, x.Portions));
        }

        /// <summary>Tổng doanh thu trong [from, toExclusive); ngày thiếu tính là 0.</summary>
        private static decimal SumRevenue(
            Dictionary<DateTime, DayAggregate> perDay, DateTime from, DateTime toExclusive)
        {
            decimal sum = 0m;
            for (var day = from; day < toExclusive; day = day.AddDays(1))
            {
                if (perDay.TryGetValue(day, out var v))
                {
                    sum += v.Revenue;
                }
            }

            return sum;
        }

        /// <summary>Chuỗi lịch sử 30 ngày gần nhất (ngày không có đơn tính là 0).</summary>
        private static void BuildHistorySeries(
            RevenueForecastDto result,
            Dictionary<DateTime, DayAggregate> perDay,
            DateTime today)
        {
            for (var day = today.AddDays(-HistoryDays); day < today; day = day.AddDays(1))
            {
                perDay.TryGetValue(day, out var v);
                result.History.Add(new ForecastHistoryPointDto
                {
                    Date = day,
                    Revenue = v?.Revenue ?? 0m,
                    OrderCount = v?.Orders ?? 0L,
                    Portions = v?.Portions ?? 0L
                });
            }
        }

        /// <summary>
        /// Pattern theo thứ trong 56 ngày: trung bình doanh thu/đơn/suất của
        /// từng thứ (T2..CN, thứ 2 trước cho UI tiếng Việt).
        /// </summary>
        private static void BuildWeekdayPattern(
            RevenueForecastDto result,
            Dictionary<DateTime, DayAggregate> perDay,
            DateTime patternStart,
            DateTime today,
            CultureInfo vi)
        {
            var overallAvg = result.AvgDailyRevenue30d;
            var weekdayStats = new Dictionary<DayOfWeek, (decimal revenue, double orders, double portions, int days)>();
            foreach (var dow in Enum.GetValues<DayOfWeek>())
            {
                weekdayStats[dow] = (0m, 0, 0, 0);
            }

            for (var day = patternStart; day < today; day = day.AddDays(1))
            {
                var stats = weekdayStats[day.DayOfWeek];
                if (perDay.TryGetValue(day, out var v))
                {
                    stats = (stats.revenue + v.Revenue, stats.orders + v.Orders, stats.portions + v.Portions, stats.days + 1);
                }
                else
                {
                    stats = (stats.revenue, stats.orders, stats.portions, stats.days + 1); // ngày mở cửa nhưng không có đơn
                }

                weekdayStats[day.DayOfWeek] = stats;
            }

            foreach (var dow in OrderedWeekdays)
            {
                var stats = weekdayStats[dow];
                var avgRev = stats.days > 0 ? stats.revenue / stats.days : 0m;
                result.WeekdayPattern.Add(new WeekdayPatternDto
                {
                    DayOfWeek = dow,
                    WeekdayName = vi.DateTimeFormat.DayNames[(int)dow],
                    Occurrences = stats.days,
                    AvgRevenue = Math.Round(avgRev, 0),
                    AvgOrders = Math.Round(stats.orders / Math.Max(stats.days, 1), 1),
                    AvgPortions = Math.Round(stats.portions / Math.Max(stats.days, 1), 1),
                    RatioVsAverage = overallAvg > 0
                        ? Math.Round((double)(avgRev / overallAvg), 2)
                        : 0
                });
            }
        }

        /// <summary>
        /// Dự báo 7 và 30 ngày tới: mỗi ngày = trung bình của đúng thứ đó trong
        /// lịch sử (cần ≥ MinWeekdayOccurrences lần xuất hiện), thiếu dữ liệu
        /// thì dùng trung bình ngày 30 ngày gần nhất.
        /// </summary>
        private static void BuildForecasts(
            RevenueForecastDto result,
            Dictionary<DateTime, DayAggregate> perDay,
            DateTime today,
            CultureInfo vi,
            double totalOrders,
            double portionsPerOrder)
        {
            for (var i = 1; i <= Forecast7d; i++)
            {
                result.Next7Days.Add(ForecastOneDay(
                    today.AddDays(i), result, perDay, vi, totalOrders, portionsPerOrder));
            }

            result.Next7DaysTotal = new ForecastTotalDto
            {
                ForecastRevenue = result.Next7Days.Sum(x => x.ForecastRevenue),
                ForecastOrders = Math.Round(result.Next7Days.Sum(x => x.ForecastOrders), 1),
                SuggestedPortions = result.Next7Days.Sum(x => x.SuggestedPortions)
            };

            var total30 = new ForecastTotalDto();
            for (var i = 1; i <= Forecast30d; i++)
            {
                var dayForecast = ForecastOneDay(
                    today.AddDays(i), result, perDay, vi, totalOrders, portionsPerOrder);
                total30.ForecastRevenue += dayForecast.ForecastRevenue;
                total30.ForecastOrders += dayForecast.ForecastOrders;
                total30.SuggestedPortions += dayForecast.SuggestedPortions;
            }

            total30.ForecastOrders = Math.Round(total30.ForecastOrders, 1);
            result.Next30DaysTotal = total30;
        }

        /// <summary>Dự báo 1 ngày từ pattern của đúng thứ đó (hoặc fallback).</summary>
        private static ForecastDayDto ForecastOneDay(
            DateTime date,
            RevenueForecastDto result,
            Dictionary<DateTime, DayAggregate> perDay,
            CultureInfo vi,
            double totalOrders,
            double portionsPerOrder)
        {
            var pattern = result.WeekdayPattern.First(x => x.DayOfWeek == date.DayOfWeek);
            var usePattern = pattern.Occurrences >= MinWeekdayOccurrences;
            var rev = usePattern ? pattern.AvgRevenue : Math.Round(result.AvgDailyRevenue30d, 0);
            var ord = usePattern
                ? pattern.AvgOrders
                : Math.Round((double)(result.AvgDailyRevenue30d / AvgOrderValue(perDay, totalOrders)), 1);
            var portions = usePattern
                ? (long)Math.Ceiling(pattern.AvgPortions)
                : (long)Math.Ceiling(ord * portionsPerOrder);

            return new ForecastDayDto
            {
                Date = date,
                WeekdayName = vi.DateTimeFormat.DayNames[(int)date.DayOfWeek],
                ForecastRevenue = rev,
                ForecastOrders = ord,
                SuggestedPortions = Math.Max(portions, 0),
                BasedOnWeekdayPattern = usePattern
            };
        }

        /// <summary>
        /// Giá trị đơn trung bình — để ước số đơn khi thiếu dữ liệu thứ.
        /// Chưa có dữ liệu thì giả định <see cref="FallbackAvgOrderValueVnd"/>.
        /// </summary>
        private static decimal AvgOrderValue(
            Dictionary<DateTime, DayAggregate> perDay, double totalOrders)
        {
            return totalOrders > 0
                ? perDay.Values.Sum(x => x.Revenue) / (decimal)totalOrders
                : FallbackAvgOrderValueVnd;
        }
    }
}
