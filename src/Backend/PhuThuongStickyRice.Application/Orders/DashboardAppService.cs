using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Shop;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using Volo.Abp.ObjectMapping;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Reports;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IDashboardAppService : IApplicationService
    {
        Task<DashboardStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null);

        Task<List<TopProductDto>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5);

        /// <summary>
        /// Item 90: new customers (first order inside the range) vs
        /// returning customers, over the selected date range.
        /// </summary>
        Task<NewReturningCustomersDto> GetNewReturningCustomersAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Public: total quantity of items sold today (Vietnam time),
        /// shown on the shop hero and landing page ("Đã bán N gói hôm nay").
        /// </summary>
        Task<long> GetTodaySoldCountAsync();

        /// <summary>
        /// Public: product ids ordered by quantity sold (all-time,
        /// non-cancelled orders). Powers the shop's "Bán chạy" sort.
        /// </summary>
        Task<List<Guid>> GetBestSellerProductIdsAsync(int top);

        /// <summary>
        /// Item 139: today's revenue vs the daily target (Vietnam date).
        /// </summary>
        Task<DailyTargetDto> GetDailyTargetAsync();

        /// <summary>
        /// Item 133: orders + revenue grouped by order source over a range.
        /// </summary>
        Task<List<SourceStatDto>> GetSourceStatsAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Wave 6d (item 36): today's vs yesterday's revenue and order count
        /// (Vietnam day boundaries), with delta percents.
        /// </summary>
        Task<TodayVsYesterdayDto> GetTodayVsYesterdayAsync();
    }

    public class DashboardAppService : ApplicationService, IDashboardAppService
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;
        private readonly IShopSettingAppService _shopSettings;

        public DashboardAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            IShopSettingAppService shopSettings)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
            _shopSettings = shopSettings;
        }

        public virtual async Task<DashboardStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var (fromDate, toDate) = NormalizeRange(from, to);

            var orderQueryable = await _orders.GetQueryableAsync();
            var productQueryable = await _products.GetQueryableAsync();

            var ranged = orderQueryable.Where(x => x.CreatedAt >= fromDate && x.CreatedAt < toDate);
            var billable = ranged.Where(x => x.Status != OrderStatus.Cancelled);

            var stats = new DashboardStatsDto
            {
                TotalOrders = await _asyncExecuter.LongCountAsync(ranged),
                PendingOrders = await _asyncExecuter.LongCountAsync(
                    orderQueryable.Where(x => x.Status == OrderStatus.Pending)),
                TotalRevenue = await _asyncExecuter.SumAsync(billable.Select(x => x.TotalAmount.Amount)),
                TotalProductsSold = await _asyncExecuter.SumAsync(
                    billable.SelectMany(x => x.Lines).Select(x => (long)x.Quantity)),
                TotalProducts = await _asyncExecuter.LongCountAsync(productQueryable)
            };

            stats.OrdersByStatus = await _asyncExecuter.ToListAsync(
                ranged.GroupBy(x => x.Status)
                    .Select(g => new OrdersByStatusDto
                    {
                        Status = g.Key.ToString(),
                        Count = g.LongCount()
                    }));

            stats.RevenueByDay = await _asyncExecuter.ToListAsync(
                billable.GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new RevenueByDayDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.TotalAmount.Amount),
                        Orders = g.LongCount()
                    })
                    .OrderBy(x => x.Date));

            var recent = await _asyncExecuter.ToListAsync(
                orderQueryable.OrderByDescending(x => x.CreatedAt).Take(5));
            stats.RecentOrders = _objectMapper.Map<List<Order>, List<OrderDto>>(recent);

            // Pre-orders the kitchen should prioritize: scheduled, not yet
            // picked up, not cancelled/completed — soonest first.
            var now = DateTime.UtcNow;
            var upcoming = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.ScheduledFor != null
                        && x.ScheduledFor >= now.AddHours(-2)
                        && !x.PickedUpAt.HasValue
                        && x.Status != OrderStatus.Cancelled
                        && x.Status != OrderStatus.Completed)
                    .OrderBy(x => x.ScheduledFor)
                    .Take(8));
            stats.UpcomingPreOrders = _objectMapper.Map<List<Order>, List<OrderDto>>(upcoming);

            // Item 86: orders still Pending after the configured minutes —
            // the dashboard raises a distinct urgent alert for these.
            var settings = await _shopSettings.GetAsync();
            var staleMinutes = settings.UnconfirmedAlertMinutes < 1 ? 15 : settings.UnconfirmedAlertMinutes;
            var staleCutoff = DateTime.UtcNow.AddMinutes(-staleMinutes);
            stats.StalePendingOrders = await _asyncExecuter.LongCountAsync(
                orderQueryable.Where(x => x.Status == OrderStatus.Pending && x.CreatedAt < staleCutoff));
            stats.StalePendingMinutes = staleMinutes;

            return stats;
        }

        public virtual async Task<DailyTargetDto> GetDailyTargetAsync()
        {
            var settings = await _shopSettings.GetAsync();
            var target = settings.DailyTargetAmount > 0 ? settings.DailyTargetAmount : 2000000;

            var todayVn = VietnamTime.Today();
            var startUtc = todayVn.AddHours(-7); // VN 00:00 → UTC
            var endUtc = startUtc.AddDays(1);

            var orderQueryable = await _orders.GetQueryableAsync();
            var revenue = await _asyncExecuter.SumAsync(
                orderQueryable
                    .Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc
                        && x.Status != OrderStatus.Cancelled)
                    .Select(x => x.TotalAmount.Amount));

            return new DailyTargetDto
            {
                TodayRevenue = revenue,
                Target = target,
                Percent = target > 0 ? (double)revenue / (double)target * 100 : 0
            };
        }

        public virtual async Task<List<SourceStatDto>> GetSourceStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var (fromDate, toDate) = NormalizeRange(from, to);
            var orderQueryable = await _orders.GetQueryableAsync();

            var rows = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.CreatedAt >= fromDate && x.CreatedAt < toDate
                        && x.Status != OrderStatus.Cancelled)
                    .GroupBy(x => x.Source)
                    .Select(g => new SourceStatDto
                    {
                        Source = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(x => x.TotalAmount.Amount)
                    })
                    .OrderByDescending(x => x.Revenue));

            return rows;
        }

        public virtual async Task<NewReturningCustomersDto> GetNewReturningCustomersAsync(
            DateTime? from = null, DateTime? to = null)
        {
            var (fromDate, toDate) = NormalizeRange(from, to);
            var orderQueryable = await _orders.GetQueryableAsync();

            // Phones (normalized) with at least one non-cancelled order in range.
            var inRange = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= fromDate && x.CreatedAt < toDate)
                    .Select(x => new { x.CustomerPhone, x.CreatedAt }));
            var inRangePhones = inRange
                .Select(x => (x.CustomerPhone ?? string.Empty).Trim())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToList();

            var result = new NewReturningCustomersDto();
            if (inRangePhones.Count == 0)
            {
                return result;
            }

            // Phones that already ordered before the range = returning.
            var beforePhones = new HashSet<string>(await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt < fromDate
                        && inRangePhones.Contains(x.CustomerPhone))
                    .Select(x => x.CustomerPhone)
                    .Distinct()));

            var newPhones = inRangePhones.Where(p => !beforePhones.Contains(p)).ToList();
            var returningPhones = inRangePhones.Where(p => beforePhones.Contains(p)).ToList();

            result.NewCustomers = newPhones.Count;
            result.ReturningCustomers = returningPhones.Count;
            result.NewCustomerOrders = inRange.Count(x => newPhones.Contains((x.CustomerPhone ?? string.Empty).Trim()));
            result.ReturningCustomerOrders = inRange.Count(x => returningPhones.Contains((x.CustomerPhone ?? string.Empty).Trim()));
            return result;
        }

        public virtual async Task<List<TopProductDto>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5)
        {
            var (fromDate, toDate) = NormalizeRange(from, to);

            var orderQueryable = await _orders.GetQueryableAsync();
            var billable = orderQueryable.Where(x =>
                x.Status != OrderStatus.Cancelled
                && x.CreatedAt >= fromDate && x.CreatedAt < toDate);

            return await _asyncExecuter.ToListAsync(
                billable.SelectMany(x => x.Lines)
                    .GroupBy(x => new { x.ProductId, x.ProductName })
                    .Select(g => new TopProductDto
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        QuantitySold = g.Sum(x => (long)x.Quantity),
                        Revenue = g.Sum(x => x.LineTotal.Amount)
                    })
                    .OrderByDescending(x => x.QuantitySold)
                    .Take(top < 1 ? 5 : top));
        }

        public virtual async Task<List<Guid>> GetBestSellerProductIdsAsync(int top)
        {
            var products = await GetTopProductsAsync(null, null, top < 1 ? 100 : top);
            return products.Select(x => x.ProductId).ToList();
        }

        /// <summary>
        /// Wave 6d (item 36): today's vs yesterday's revenue + order count
        /// using Vietnam day boundaries (CreatedAt is UTC).
        /// </summary>
        public virtual async Task<TodayVsYesterdayDto> GetTodayVsYesterdayAsync()
        {
            var todayVn = VietnamTime.Today();
            var startUtc = todayVn.AddHours(-7); // VN 00:00 today → UTC
            var yesterdayStartUtc = startUtc.AddDays(-1);
            var endUtc = startUtc.AddDays(1);

            var orderQueryable = await _orders.GetQueryableAsync();
            var billable = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled);

            var todayQ = billable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc);
            var yesterdayQ = billable.Where(x => x.CreatedAt >= yesterdayStartUtc && x.CreatedAt < startUtc);

            var todayRevenue = await _asyncExecuter.SumAsync(todayQ.Select(x => x.TotalAmount.Amount));
            var yesterdayRevenue = await _asyncExecuter.SumAsync(yesterdayQ.Select(x => x.TotalAmount.Amount));
            var todayOrders = await _asyncExecuter.LongCountAsync(todayQ);
            var yesterdayOrders = await _asyncExecuter.LongCountAsync(yesterdayQ);

            return new TodayVsYesterdayDto
            {
                TodayRevenue = todayRevenue,
                YesterdayRevenue = yesterdayRevenue,
                TodayOrders = todayOrders,
                YesterdayOrders = yesterdayOrders,
                RevenueDeltaPercent = DeltaPercent(todayRevenue, yesterdayRevenue),
                OrdersDeltaPercent = DeltaPercent(todayOrders, yesterdayOrders)
            };
        }

        private static double DeltaPercent(decimal today, decimal yesterday)
        {
            if (yesterday == 0)
            {
                return today > 0 ? 100 : 0;
            }

            return (double)(today - yesterday) / (double)yesterday * 100;
        }

        public virtual async Task<long> GetTodaySoldCountAsync()
        {
            // The shop runs on Vietnam time (UTC+7); CreatedAt is UTC.
            var startOfTodayUtc = DateTime.UtcNow.AddHours(7).Date.AddHours(-7);

            var orderQueryable = await _orders.GetQueryableAsync();
            var billable = orderQueryable.Where(x =>
                x.Status != OrderStatus.Cancelled
                && x.CreatedAt >= startOfTodayUtc);

            return await _asyncExecuter.SumAsync(
                billable.SelectMany(x => x.Lines).Select(x => (long)x.Quantity));
        }

        /// <summary>
        /// Default range = last 7 days (old behavior). The end is exclusive
        /// and pushed to the end of the "to" day so the whole day is included.
        /// </summary>
        private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
        {
            var toDate = (to?.Date ?? DateTime.UtcNow.Date).AddDays(1);
            var fromDate = from?.Date ?? toDate.AddDays(-7);
            if (fromDate > toDate)
            {
                fromDate = toDate.AddDays(-7);
            }

            return (fromDate, toDate);
        }
    }
}
