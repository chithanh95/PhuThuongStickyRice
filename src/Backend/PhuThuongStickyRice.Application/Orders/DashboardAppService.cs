using Microsoft.Extensions.Caching.Memory;
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
        /// BA shop item 4: total quantity sold per product (all time,
        /// non-cancelled orders). Powers the "Đã bán N suất" social proof
        /// on product cards. Quantities sold are not sensitive.
        /// </summary>
        Task<Dictionary<Guid, long>> GetSoldQuantitiesAsync();

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

    /// <summary>
    /// Dashboard admin và các số liệu công khai cho shop: thống kê tổng quan
    /// theo ngày (cache 45s), top sản phẩm, mục tiêu doanh thu ngày, khách
    /// mới/cũ, doanh thu theo nguồn, so sánh hôm nay–hôm qua, số suất đã bán
    /// hôm nay. Số liệu marketing (đã bán N gói) cache ngắn vì không cần real-time.
    /// </summary>
    public class DashboardAppService : ApplicationService, IDashboardAppService
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;
        private readonly IShopSettingAppService _shopSettings;
        private readonly Products.IProductCatalogCache _catalogCache;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;

        public DashboardAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            IShopSettingAppService shopSettings,
            Products.IProductCatalogCache catalogCache,
            Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
            _shopSettings = shopSettings;
            _catalogCache = catalogCache;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Tổng quan dashboard theo khoảng ngày: tổng đơn/doanh thu/suất bán,
        /// đơn theo trạng thái, doanh thu theo ngày, 5 đơn mới nhất, đơn đặt
        /// trước sắp tới, và số đơn chờ xác nhận quá hạn để cảnh báo.
        /// </summary>
        public virtual async Task<DashboardStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            // P7: cache dashboard stats 45s (11 queries -> 0 on hit).
            var cacheKey = $"dashboard-stats:{from:yyyyMMddHHmm}:{to:yyyyMMddHHmm}";
            if (_memoryCache.TryGetValue(cacheKey, out object cachedObj)
                && cachedObj is DashboardStatsDto cachedStats)
            {
                return cachedStats;
            }

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

            // Đơn đặt trước (bếp ưu tiên xử lý): có giờ hẹn từ 2 tiếng trước tới
            // nay (đơn quá giờ hẹn mà khách chưa bấm nhận vẫn hiện để bếp xử
            // lý), chưa nhận hàng, chưa hủy/hoàn thành — giờ hẹn sớm nhất lên trước.
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

            // Đơn còn Pending quá số phút cấu hình: dashboard bật cảnh báo khẩn
            // riêng để quán không bỏ sót đơn chưa xác nhận.
            var settings = await _shopSettings.GetAsync();
            var staleMinutes = settings.UnconfirmedAlertMinutes < 1 ? 15 : settings.UnconfirmedAlertMinutes;
            var staleCutoff = DateTime.UtcNow.AddMinutes(-staleMinutes);
            stats.StalePendingOrders = await _asyncExecuter.LongCountAsync(
                orderQueryable.Where(x => x.Status == OrderStatus.Pending && x.CreatedAt < staleCutoff));
            stats.StalePendingMinutes = staleMinutes;

            _memoryCache.Set(cacheKey, stats, TimeSpan.FromSeconds(45));
            return stats;
        }

        /// <summary>
        /// Doanh thu hôm nay (tính theo ngày VN) so với mục tiêu ngày trong cài
        /// đặt shop (mặc định 2.000.000₫ khi chưa cài). CreatedAt lưu UTC nên
        /// trừ 7 giờ để ra đầu ngày VN.
        /// </summary>
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

        /// <summary>
        /// Đơn + doanh thu nhóm theo nguồn đặt (web/Zalo/trực tiếp...) trong
        /// khoảng ngày, sắp xếp theo doanh thu giảm dần.
        /// </summary>
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
            // Batch the IN clause (<= 1000 params/query; SQL Server allows ~2100).
            var beforePhones = new HashSet<string>();
            foreach (var batch in inRangePhones.Chunk(1000))
            {
                var found = await _asyncExecuter.ToListAsync(
                    orderQueryable
                        .Where(x => x.Status != OrderStatus.Cancelled
                            && x.CreatedAt < fromDate
                            && batch.Contains(x.CustomerPhone))
                        .Select(x => x.CustomerPhone)
                        .Distinct());
                foreach (var p in found)
                {
                    beforePhones.Add(p);
                }
            }

            var newPhones = new HashSet<string>(inRangePhones.Where(p => !beforePhones.Contains(p)));
            var returningPhones = new HashSet<string>(inRangePhones.Where(p => beforePhones.Contains(p)));

            result.NewCustomers = newPhones.Count;
            result.ReturningCustomers = returningPhones.Count;
            result.NewCustomerOrders = inRange.Count(x => newPhones.Contains((x.CustomerPhone ?? string.Empty).Trim()));
            result.ReturningCustomerOrders = inRange.Count(x => returningPhones.Contains((x.CustomerPhone ?? string.Empty).Trim()));
            return result;
        }

        /// <summary>
        /// Top N sản phẩm bán chạy theo số lượng trong khoảng ngày (đơn hủy
        /// bị loại). Mặc định top 5 khi không truyền hoặc top ≤ 0.
        /// </summary>
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

        /// <summary>
        /// Public: id các sản phẩm bán chạy all-time (đơn hủy bị loại) — cấp
        /// dữ liệu cho sắp xếp "Bán chạy" ở shop, không cần login.
        /// </summary>
        public virtual async Task<List<Guid>> GetBestSellerProductIdsAsync(int top)
        {
            // perf-hunt 2026-10-19: GET /api/products/best-sellers is public and hit on
            // every shop menu load. The all-time OrderLines aggregate is expensive, so
            // cache the top-200 list (same 5-min TTL semantics as GetSoldQuantitiesAsync)
            // and slice the requested top in memory. Clamp top to avoid huge payloads.
            var clamped = top < 1 ? 100 : Math.Min(top, 200);
            var products = await _catalogCache.GetOrAddAsync(
                "best-sellers",
                () => GetTopProductsAsync(null, null, 200));
            return products.Take(clamped).Select(x => x.ProductId).ToList();
        }

        /// <summary>
        /// Public: tổng số lượng đã bán của từng sản phẩm (all-time, đơn hủy
        /// bị loại) — hiển thị "Đã bán N suất" trên thẻ sản phẩm ở shop. Không
        /// chứa thông tin nhạy cảm nên public.
        /// </summary>
        public virtual async Task<Dictionary<Guid, long>> GetSoldQuantitiesAsync()
        {
            return await _catalogCache.GetOrAddAsync("sold-quantities", QuerySoldQuantitiesAsync);
        }

        private async Task<Dictionary<Guid, long>> QuerySoldQuantitiesAsync()
        {
            var orderQueryable = await _orders.GetQueryableAsync();
            var billable = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled);

            var rows = await _asyncExecuter.ToListAsync(
                billable.SelectMany(x => x.Lines)
                    .GroupBy(x => x.ProductId)
                    .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => (long)x.Quantity) }));
            return rows.ToDictionary(x => x.ProductId, x => x.Qty);
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

        /// <summary>
        /// Phần trăm thay đổi hôm nay so với hôm qua. Hôm qua không bán được
        /// gì (mẫu số 0) mà hôm nay có bán thì coi như +100%, tránh chia cho 0.
        /// </summary>
        private static double DeltaPercent(decimal today, decimal yesterday)
        {
            if (yesterday == 0)
            {
                return today > 0 ? 100 : 0;
            }

            return (double)(today - yesterday) / (double)yesterday * 100;
        }

        /// <summary>
        /// Public: tổng số suất bán hôm nay (theo ngày VN) — hiển thị "Đã bán
        /// N gói hôm nay" ở hero shop/landing. Chỉ là số marketing, cache 60s
        /// để mỗi lượt tải trang không phải chạy SUM nặng.
        /// </summary>
        public virtual async Task<long> GetTodaySoldCountAsync()
        {
            // Perf: "Đã bán N gói hôm nay" is shown on every shop/landing
            // load. Cache 60s keyed by VN date — a marketing number, not
            // real-time; the heavy SUM over today's order lines must not
            // run per page view.
            var vnDate = DateTime.UtcNow.AddHours(7).Date;
            var cacheKey = $"dashboard:today-sold:{vnDate:yyyyMMdd}";
            if (_memoryCache.TryGetValue(cacheKey, out long cached))
            {
                return cached;
            }

            // The shop runs on Vietnam time (UTC+7); CreatedAt is UTC.
            var startOfTodayUtc = vnDate.AddHours(-7);

            var orderQueryable = await _orders.GetQueryableAsync();
            var billable = orderQueryable.Where(x =>
                x.Status != OrderStatus.Cancelled
                && x.CreatedAt >= startOfTodayUtc);

            var result = await _asyncExecuter.SumAsync(
                billable.SelectMany(x => x.Lines).Select(x => (long)x.Quantity));

            _memoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(60));
            return result;
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
