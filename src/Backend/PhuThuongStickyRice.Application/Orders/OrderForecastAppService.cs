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
    public interface IOrderForecastAppService : IApplicationService
    {
        /// <summary>
        /// Admin: revenue forecast based on historical daily averages.
        /// </summary>
        Task<RevenueForecastDto> GetRevenueForecastAsync();
    }

    /// <summary>
    /// Revenue forecast. Heuristic, deliberately simple and documented:
    /// <list type="bullet">
    /// <item>Look back 56 completed days (8 full weeks) of non-cancelled orders.</item>
    /// <item>Compute per-weekday averages (Thứ 2..Chủ nhật) — e.g. Saturdays
    /// are often ~2x a normal day.</item>
    /// <item>Forecast each future day from its weekday average; when a
    /// weekday has fewer than 2 data points, fall back to the 30-day
    /// overall daily average.</item>
    /// <item>Moving averages (7d/30d) are computed over completed days only —
    /// today is partial and excluded.</item>
    /// </list>
    /// Zero-revenue days count as 0 (the shop is assumed open daily); the
    /// method note on the DTO says so. This is an estimate, not a guarantee.
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
        private async Task<Dictionary<DateTime, DayAggregate>> LoadDailyAggregatesAsync(
            DateTime today, DateTime patternStart)
        {
            var fetchFromUtc = patternStart.AddDays(-1); // margin cho múi giờ

            var queryable = await _orders.GetQueryableAsync();
            var raw = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= fetchFromUtc
                        && x.CreatedAt < today // loại hôm nay vì chưa bán xong
                        && x.Status != OrderStatus.Cancelled)
                    .Select(x => new
                    {
                        x.CreatedAt,
                        Revenue = x.TotalAmount.Amount,
                        Portions = x.Lines.Sum(l => (long)l.Quantity)
                    }));

            var perDay = new Dictionary<DateTime, DayAggregate>();
            foreach (var o in raw)
            {
                var local = VietnamTime.ToVietnamTime(o.CreatedAt).Date;
                if (local < patternStart || local >= today)
                {
                    continue;
                }

                if (perDay.TryGetValue(local, out var cur))
                {
                    perDay[local] = cur with
                    {
                        Revenue = cur.Revenue + o.Revenue,
                        Orders = cur.Orders + 1,
                        Portions = cur.Portions + o.Portions
                    };
                }
                else
                {
                    perDay[local] = new DayAggregate(o.Revenue, 1, o.Portions);
                }
            }

            return perDay;
        }

        /// <summary>Tổng doanh thu trong [from, toExclusive); ngày thiếu tính là 0.</summary>
        private static decimal SumRevenue(
            Dictionary<DateTime, DayAggregate> perDay, DateTime from, DateTime toExclusive)
        {
            decimal sum = 0m;
            for (var d = from; d < toExclusive; d = d.AddDays(1))
            {
                if (perDay.TryGetValue(d, out var v))
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
            for (var d = today.AddDays(-HistoryDays); d < today; d = d.AddDays(1))
            {
                perDay.TryGetValue(d, out var v);
                result.History.Add(new ForecastHistoryPointDto
                {
                    Date = d,
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
            foreach (DayOfWeek dow in Enum.GetValues(typeof(DayOfWeek)))
            {
                weekdayStats[dow] = (0m, 0, 0, 0);
            }

            for (var d = patternStart; d < today; d = d.AddDays(1))
            {
                var stats = weekdayStats[d.DayOfWeek];
                if (perDay.TryGetValue(d, out var v))
                {
                    stats = (stats.revenue + v.Revenue, stats.orders + v.Orders, stats.portions + v.Portions, stats.days + 1);
                }
                else
                {
                    stats = (stats.revenue, stats.orders, stats.portions, stats.days + 1); // ngày mở cửa nhưng không có đơn
                }

                weekdayStats[d.DayOfWeek] = stats;
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
