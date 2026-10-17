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
        private const int PatternDays = 56; // 8 weeks
        private const int MinWeekdayOccurrences = 2;

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
            var today = VietnamTime.Today;
            var patternStart = today.AddDays(-PatternDays); // inclusive
            var fetchFromUtc = patternStart.AddDays(-1); // margin for tz

            var queryable = await _orders.GetQueryableAsync();
            var raw = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= fetchFromUtc
                        && x.CreatedAt < today // exclude partial today
                        && x.Status != OrderStatus.Cancelled)
                    .Select(x => new
                    {
                        x.CreatedAt,
                        Revenue = x.TotalAmount.Amount,
                        Portions = x.Lines.Sum(l => (long)l.Quantity)
                    }));

            // Aggregate per Vietnam-local day.
            var perDay = new Dictionary<DateTime, (decimal revenue, long orders, long portions)>();
            foreach (var o in raw)
            {
                var local = o.CreatedAt.AddHours(7).Date;
                if (local < patternStart || local >= today)
                {
                    continue;
                }

                if (perDay.TryGetValue(local, out var cur))
                {
                    perDay[local] = (cur.revenue + o.Revenue, cur.orders + 1, cur.portions + o.Portions);
                }
                else
                {
                    perDay[local] = (o.Revenue, 1, o.Portions);
                }
            }

            var vi = CultureInfo.GetCultureInfo("vi-VN");
            var result = new RevenueForecastDto
            {
                GeneratedAt = DateTime.UtcNow,
                HistoryDays = PatternDays,
                DaysWithOrders = perDay.Count
            };

            // ---- Moving averages over completed days ----
            decimal SumRevenue(DateTime from, DateTime toExclusive)
            {
                decimal sum = 0m;
                for (var d = from; d < toExclusive; d = d.AddDays(1))
                {
                    if (perDay.TryGetValue(d, out var v))
                    {
                        sum += v.revenue;
                    }
                }

                return sum;
            }

            result.AvgDailyRevenue7d = SumRevenue(today.AddDays(-7), today) / 7m;
            result.AvgDailyRevenue30d = SumRevenue(today.AddDays(-30), today) / 30m;

            // ---- History series: last 30 completed days, zeros included ----
            for (var d = today.AddDays(-30); d < today; d = d.AddDays(1))
            {
                perDay.TryGetValue(d, out var v);
                result.History.Add(new ForecastHistoryPointDto
                {
                    Date = d,
                    Revenue = v.revenue,
                    OrderCount = v.orders,
                    Portions = v.portions
                });
            }

            // ---- Per-weekday pattern over the 56-day window ----
            var overallAvg = result.AvgDailyRevenue30d;
            var weekdayStats = new Dictionary<DayOfWeek, (decimal rev, double ord, double por, int n)>();
            foreach (DayOfWeek dow in Enum.GetValues(typeof(DayOfWeek)))
            {
                weekdayStats[dow] = (0m, 0, 0, 0);
            }

            for (var d = patternStart; d < today; d = d.AddDays(1))
            {
                var s = weekdayStats[d.DayOfWeek];
                if (perDay.TryGetValue(d, out var v))
                {
                    s = (s.rev + v.revenue, s.ord + v.orders, s.por + v.portions, s.n + 1);
                }
                else
                {
                    s = (s.rev, s.ord, s.por, s.n + 1); // open day, zero revenue
                }

                weekdayStats[d.DayOfWeek] = s;
            }

            // Monday-first ordering for the Vietnamese UI.
            var orderedDays = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };
            foreach (var dow in orderedDays)
            {
                var s = weekdayStats[dow];
                var avgRev = s.n > 0 ? s.rev / s.n : 0m;
                result.WeekdayPattern.Add(new WeekdayPatternDto
                {
                    DayOfWeek = dow,
                    WeekdayName = vi.DateTimeFormat.DayNames[(int)dow],
                    Occurrences = s.n,
                    AvgRevenue = Math.Round(avgRev, 0),
                    AvgOrders = Math.Round(s.ord / Math.Max(s.n, 1), 1),
                    AvgPortions = Math.Round(s.por / Math.Max(s.n, 1), 1),
                    RatioVsAverage = overallAvg > 0
                        ? Math.Round((double)(avgRev / overallAvg), 2)
                        : 0
                });
            }

            // Overall portions per order, for the fallback path.
            var totalPortions = perDay.Values.Sum(x => (double)x.portions);
            var totalOrders = perDay.Values.Sum(x => (double)x.orders);
            var portionsPerOrder = totalOrders > 0 ? totalPortions / totalOrders : 1.5;

            ForecastDayDto ForecastOne(DateTime date)
            {
                var pattern = result.WeekdayPattern.First(x => x.DayOfWeek == date.DayOfWeek);
                var usePattern = pattern.Occurrences >= MinWeekdayOccurrences;
                var rev = usePattern ? pattern.AvgRevenue : Math.Round(overallAvg, 0);
                var ord = usePattern ? pattern.AvgOrders : Math.Round((double)(overallAvg / AvgOrderValue()), 1);
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

            decimal AvgOrderValue()
            {
                return totalOrders > 0
                    ? perDay.Values.Sum(x => x.revenue) / (decimal)totalOrders
                    : 30000m;
            }

            // ---- Next 7 days ----
            for (var i = 1; i <= 7; i++)
            {
                result.Next7Days.Add(ForecastOne(today.AddDays(i)));
            }

            result.Next7DaysTotal = new ForecastTotalDto
            {
                ForecastRevenue = result.Next7Days.Sum(x => x.ForecastRevenue),
                ForecastOrders = Math.Round(result.Next7Days.Sum(x => x.ForecastOrders), 1),
                SuggestedPortions = result.Next7Days.Sum(x => x.SuggestedPortions)
            };

            // ---- Next 30 days ----
            var total30 = new ForecastTotalDto();
            for (var i = 1; i <= 30; i++)
            {
                var f = ForecastOne(today.AddDays(i));
                total30.ForecastRevenue += f.ForecastRevenue;
                total30.ForecastOrders += f.ForecastOrders;
                total30.SuggestedPortions += f.SuggestedPortions;
            }

            total30.ForecastOrders = Math.Round(total30.ForecastOrders, 1);
            result.Next30DaysTotal = total30;

            result.MethodNote =
                $"Trung bình trượt trên {PatternDays} ngày đã qua (đơn đã hủy bị loại, " +
                $"ngày không có đơn tính là 0). Dự báo mỗi ngày = trung bình của đúng " +
                $"thứ đó trong lịch sử (cần ≥ {MinWeekdayOccurrences} lần xuất hiện), " +
                $"nếu thiếu dữ liệu thì dùng trung bình ngày 30 ngày gần nhất " +
                $"({Math.Round(overallAvg, 0):N0}₫/ngày). Số suất gợi ý làm tròn lên.";

            result.Disclaimer =
                "Dự báo dựa trên trung bình lịch sử, không chính xác tuyệt đối. " +
                "Ngày lễ, tiệc lớn, thời tiết xấu hoặc món mới có thể làm thực tế " +
                "lệch nhiều — hãy cộng thêm dự phòng theo kinh nghiệm của quán.";

            return result;
        }
    }
}
