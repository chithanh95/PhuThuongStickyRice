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
using PhuThuongStickyRice.Application.Reports;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Hợp đồng dịch vụ thống kê dashboard (admin): tổng quan doanh thu/đơn
    /// hàng, top món bán chạy, khách mới so với khách quay lại. Kết quả
    /// được cache 45 giây vì gọi trên mỗi lần mở dashboard.
    /// </summary>
    public interface IDashboardAppService : IApplicationService
    {
        /// <summary>
        /// Admin: thống kê tổng quan (doanh thu, số đơn, giá trị đơn trung
        /// bình...) trong khoảng thời gian. Khoảng trống mặc định theo ngày.
        /// </summary>
        /// <param name="from">Từ ngày (null = không giới hạn dưới).</param>
        /// <param name="to">Đến ngày (null = không giới hạn trên).</param>
        /// <returns>DTO thống kê tổng quan dashboard.</returns>
        Task<DashboardStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Admin: danh sách món bán chạy nhất trong khoảng thời gian.
        /// </summary>
        /// <param name="from">Từ ngày (null = không giới hạn dưới).</param>
        /// <param name="to">Đến ngày (null = không giới hạn trên).</param>
        /// <param name="top">Số món lấy (mặc định 5).</param>
        /// <returns>Danh sách món kèm số lượng/doanh thu, bán nhiều nhất trước.</returns>
        Task<List<TopProductDto>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5);

        /// <summary>
        /// Item 90: new customers (first order inside the range) vs
        /// returning customers, over the selected date range.
        /// </summary>
        Task<NewAndReturningCustomersDto> GetNewReturningCustomersAsync(DateTime? from = null, DateTime? to = null);

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
        private readonly IMemoryCache _memoryCache;

        /// <summary>TTL cache cho thống kê dashboard (giây) — query nặng, số liệu không cần real-time.</summary>
        private const int DashboardStatsCacheSeconds = 45;
        /// <summary>TTL cache cho "Đã bán N gói hôm nay" (giây) — số marketing hiển thị mỗi lần tải trang.</summary>
        private const int TodaySoldCacheSeconds = 60;
        /// <summary>Số phút mặc định coi đơn Pending là "quá hạn chưa xác nhận" khi shop chưa cấu hình.</summary>
        private const int DefaultStalePendingMinutes = 15;
        /// <summary>Mục tiêu doanh thu ngày mặc định (₫) khi shop chưa cấu hình.</summary>
        private const decimal DefaultDailyTargetAmount = 2000000;
        /// <summary>Số đơn mới nhất hiển thị trên dashboard.</summary>
        private const int RecentOrdersTake = 5;
        /// <summary>Số đơn đặt trước sắp tới hiển thị trên dashboard cho bếp.</summary>
        private const int UpcomingPreOrdersTake = 8;
        /// <summary>Số giờ nhìn lại quá khứ khi tìm đơn đặt trước (đơn quá giờ hẹn mà khách chưa nhận vẫn hiện cho bếp xử lý).</summary>
        private const int PreOrderLookbackHours = 2;
        /// <summary>Số sản phẩm mặc định của GetTopProductsAsync khi không truyền hoặc top ≤ 0.</summary>
        private const int DefaultTopProducts = 5;
        /// <summary>Số sản phẩm mặc định của API bán chạy khi client không truyền top.</summary>
        private const int BestSellersDefaultTop = 100;
        /// <summary>Số lượng tối đa bản ghi bán chạy lưu trong cache (cắt payload trả về).</summary>
        private const int BestSellersMaxTop = 200;
        /// <summary>Hệ số quy đổi tỉ lệ sang phần trăm (dùng chung cho các chỉ số % trên dashboard).</summary>
        private const double PercentMultiplier = 100;

        public DashboardAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            IShopSettingAppService shopSettings,
            Products.IProductCatalogCache catalogCache,
            IMemoryCache memoryCache)
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

            var (fromDate, toDate) = DateRangeNormalizer.NormalizeRange(from, to);
            var stats = await BuildDashboardStatsAsync(fromDate, toDate);

            _memoryCache.Set(cacheKey, stats, TimeSpan.FromSeconds(DashboardStatsCacheSeconds));
            return stats;
        }

        /// <summary>
        /// Dựng toàn bộ số liệu dashboard cho khoảng ngày: gọi các truy vấn
        /// con rồi ghép vào <see cref="DashboardStatsDto"/>. Tách riêng để
        /// GetStatsAsync chỉ còn lo cache.
        /// </summary>
        private async Task<DashboardStatsDto> BuildDashboardStatsAsync(DateTime fromDate, DateTime toDate)
        {
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

            stats.OrdersByStatus = await LoadOrdersByStatusAsync(ranged);
            stats.RevenueByDay = await LoadRevenueByDayAsync(billable);
            stats.RecentOrders = await LoadRecentOrdersAsync(orderQueryable);
            stats.UpcomingPreOrders = await LoadUpcomingPreOrdersAsync(orderQueryable);
            (stats.StalePendingOrders, stats.StalePendingMinutes) = await LoadStalePendingAsync(orderQueryable);

            return stats;
        }

        /// <summary>Số đơn theo từng trạng thái trong khoảng ngày.</summary>
        private async Task<List<OrdersByStatusDto>> LoadOrdersByStatusAsync(IQueryable<Order> ranged) =>
            await _asyncExecuter.ToListAsync(
                ranged.GroupBy(x => x.Status)
                    .Select(g => new OrdersByStatusDto
                    {
                        Status = g.Key.ToString(),
                        Count = g.LongCount()
                    }));

        /// <summary>Doanh thu + số đơn theo từng ngày trong khoảng (đơn hủy bị loại).</summary>
        private async Task<List<RevenueByDayDto>> LoadRevenueByDayAsync(IQueryable<Order> billable) =>
            await _asyncExecuter.ToListAsync(
                billable.GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new RevenueByDayDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.TotalAmount.Amount),
                        Orders = g.LongCount()
                    })
                    .OrderBy(x => x.Date));

        /// <summary>Các đơn mới nhất (mọi trạng thái) để dashboard hiển thị nhanh.</summary>
        private async Task<List<OrderDto>> LoadRecentOrdersAsync(IQueryable<Order> orderQueryable)
        {
            var recent = await _asyncExecuter.ToListAsync(
                orderQueryable.OrderByDescending(x => x.CreatedAt).Take(RecentOrdersTake));
            return _objectMapper.Map<List<Order>, List<OrderDto>>(recent);
        }

        /// <summary>
        /// Đơn đặt trước sắp tới cho bếp ưu tiên xử lý: có giờ hẹn từ
        /// <see cref="PreOrderLookbackHours"/> giờ trước tới nay (đơn quá giờ
        /// hẹn mà khách chưa bấm nhận vẫn hiện để bếp xử lý), chưa nhận hàng,
        /// chưa hủy/hoàn thành — giờ hẹn sớm nhất lên trước.
        /// </summary>
        private async Task<List<OrderDto>> LoadUpcomingPreOrdersAsync(IQueryable<Order> orderQueryable)
        {
            var now = DateTime.UtcNow;
            var upcoming = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.ScheduledFor != null
                        && x.ScheduledFor >= now.AddHours(-PreOrderLookbackHours)
                        && !x.PickedUpAt.HasValue
                        && x.Status != OrderStatus.Cancelled
                        && x.Status != OrderStatus.Completed)
                    .OrderBy(x => x.ScheduledFor)
                    .Take(UpcomingPreOrdersTake));
            return _objectMapper.Map<List<Order>, List<OrderDto>>(upcoming);
        }

        /// <summary>
        /// Số đơn còn Pending quá số phút cấu hình (mặc định
        /// <see cref="DefaultStalePendingMinutes"/> phút): dashboard bật cảnh
        /// báo khẩn riêng để quán không bỏ sót đơn chưa xác nhận.
        /// </summary>
        private async Task<(long StaleCount, int StaleMinutes)> LoadStalePendingAsync(IQueryable<Order> orderQueryable)
        {
            var settings = await _shopSettings.GetAsync();
            var staleMinutes = settings.UnconfirmedAlertMinutes < 1
                ? DefaultStalePendingMinutes
                : settings.UnconfirmedAlertMinutes;
            var staleCutoff = DateTime.UtcNow.AddMinutes(-staleMinutes);
            var count = await _asyncExecuter.LongCountAsync(
                orderQueryable.Where(x => x.Status == OrderStatus.Pending && x.CreatedAt < staleCutoff));
            return (count, staleMinutes);
        }

        /// <summary>
        /// Doanh thu hôm nay (tính theo ngày VN) so với mục tiêu ngày trong cài
        /// đặt shop (mặc định 2.000.000₫ khi chưa cài). CreatedAt lưu UTC nên
        /// trừ <see cref="VietnamTime.UtcOffsetHours"/> giờ để ra đầu ngày VN.
        /// </summary>
        public virtual async Task<DailyTargetDto> GetDailyTargetAsync()
        {
            var settings = await _shopSettings.GetAsync();
            var target = settings.DailyTargetAmount > 0 ? settings.DailyTargetAmount : DefaultDailyTargetAmount;

            var todayVn = VietnamTime.Today();
            // Biên ngày VN → UTC: dùng helper chung để mọi chỗ tính giống
            // hệt nhau (trước đây mỗi method tự cộng/trừ giờ thủ công).
            var startUtc = VietnamTime.DayStartUtc(todayVn); // VN 00:00 → UTC
            var endUtc = VietnamTime.DayEndExclusiveUtc(todayVn);

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
                Percent = target > 0 ? (double)revenue / (double)target * PercentMultiplier : 0
            };
        }

        /// <summary>
        /// Đơn + doanh thu nhóm theo nguồn đặt (web/Zalo/trực tiếp...) trong
        /// khoảng ngày, sắp xếp theo doanh thu giảm dần.
        /// </summary>
        public virtual async Task<List<SourceStatDto>> GetSourceStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            var (fromDate, toDate) = DateRangeNormalizer.NormalizeRange(from, to);
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

        /// <summary>
        /// Khách mới (có đơn đầu tiên trong khoảng ngày) so với khách cũ
        /// (đã có đơn trước đó), tính trên khoảng ngày đã chọn (Item 90).
        /// </summary>
        public virtual async Task<NewAndReturningCustomersDto> GetNewReturningCustomersAsync(
            DateTime? from = null, DateTime? to = null)
        {
            var (fromDate, toDate) = DateRangeNormalizer.NormalizeRange(from, to);
            var orderQueryable = await _orders.GetQueryableAsync();

            // perf-hunt 2026-10-19: gộp theo SĐT ngay trên SQL (1 dòng/SĐT kèm
            // số đơn) thay vì nạp toàn bộ đơn trong khoảng ngày vào memory —
            // khoảng ngày càng rộng thì bản cũ càng nặng. Tập SĐT (đã Trim,
            // loại rỗng) GIONG HET bản cũ nên phân loại mới/cũ và số đơn theo
            // từng nhóm không đổi; FindReturningPhonesAsync quét lịch sử 1
            // lần duy nhất bằng subquery thay vì chia batch.
            var inRangeGroups = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= fromDate && x.CreatedAt < toDate)
                    .GroupBy(x => x.CustomerPhone.Trim())
                    .Select(g => new { Phone = g.Key, OrderCount = g.LongCount() }));
            var inRangePhones = inRangeGroups
                .Where(x => !string.IsNullOrEmpty(x.Phone))
                .Select(x => x.Phone)
                .ToList();

            var result = new NewAndReturningCustomersDto();
            if (inRangePhones.Count == 0)
            {
                return result;
            }

            var beforePhones = await FindReturningPhonesAsync(orderQueryable, fromDate, toDate);

            var newPhones = new HashSet<string>(inRangePhones.Where(p => !beforePhones.Contains(p)));
            var returningPhones = new HashSet<string>(inRangePhones.Where(p => beforePhones.Contains(p)));

            result.NewCustomers = newPhones.Count;
            result.ReturningCustomers = returningPhones.Count;
            result.NewCustomerOrders = inRangeGroups
                .Where(x => newPhones.Contains(x.Phone)).Sum(x => x.OrderCount);
            result.ReturningCustomerOrders = inRangeGroups
                .Where(x => returningPhones.Contains(x.Phone)).Sum(x => x.OrderCount);
            return result;
        }

        /// <summary>
        /// Tìm các SĐT đã có đơn (không hủy) trước mốc fromDate — tức là
        /// khách cũ. Dùng subquery IN (SELECT ...) nên chỉ cần 1 lần quét
        /// lịch sử, không còn chia batch: giới hạn ~2100 tham số của SQL
        /// Server chỉ áp dụng khi nhét list literal vào mệnh đề IN, còn
        /// subquery thì không.
        /// </summary>
        private async Task<HashSet<string>> FindReturningPhonesAsync(
            IQueryable<Order> orderQueryable, DateTime fromDate, DateTime toDate)
        {
            // perf-hunt 2026-10-19: gộp ceil(N/1000) lần quét toàn bộ lịch sử
            // thành 1 query duy nhất. So sánh sau khi Trim cả 2 vế cho nhất
            // quán với GroupBy ở GetNewReturningCustomersAsync (bản cũ so
            // sánh SĐT thô của lịch sử với SĐT đã trim trong khoảng ngày nên
            // có thể bỏ sót khách cũ khi SĐT dính khoảng trắng thừa).
            var inRangePhones = orderQueryable
                .Where(x => x.Status != OrderStatus.Cancelled
                    && x.CreatedAt >= fromDate && x.CreatedAt < toDate
                    && !string.IsNullOrEmpty(x.CustomerPhone))
                .Select(x => x.CustomerPhone.Trim())
                .Distinct();

            var found = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt < fromDate
                        && inRangePhones.Contains(x.CustomerPhone.Trim()))
                    .Select(x => x.CustomerPhone.Trim())
                    .Distinct());

            return new HashSet<string>(found);
        }

        /// <summary>
        /// Top N sản phẩm bán chạy theo số lượng trong khoảng ngày (đơn hủy
        /// bị loại). Mặc định top 5 khi không truyền hoặc top ≤ 0.
        /// </summary>
        public virtual async Task<List<TopProductDto>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5)
        {
            var (fromDate, toDate) = DateRangeNormalizer.NormalizeRange(from, to);

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
                    .Take(top < 1 ? DefaultTopProducts : top));
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
            var clamped = top < 1 ? BestSellersDefaultTop : Math.Min(top, BestSellersMaxTop);
            var products = await _catalogCache.GetOrAddAsync(
                "best-sellers",
                () => GetTopProductsAsync(null, null, BestSellersMaxTop));
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
        /// Kết quả tổng hợp doanh thu/số đơn hôm nay + hôm qua trong 1 câu SQL
        /// (SUM/COUNT có điều kiện), thay vì 4 round-trip quét cùng khoảng ngày.
        /// </summary>
        private sealed class TodayVsYesterdayAggregate
        {
            public decimal? TodayRevenue { get; set; }

            public decimal? YesterdayRevenue { get; set; }

            public long TodayOrders { get; set; }

            public long YesterdayOrders { get; set; }
        }

        /// <summary>
        /// Wave 6d (item 36): today's vs yesterday's revenue + order count
        /// using Vietnam day boundaries (CreatedAt is UTC).
        /// </summary>
        public virtual async Task<TodayVsYesterdayDto> GetTodayVsYesterdayAsync()
        {
            var todayVn = VietnamTime.Today();
            // Dùng helper chung cho biên ngày VN → UTC (xem GetDailyTargetAsync).
            var startUtc = VietnamTime.DayStartUtc(todayVn); // VN 00:00 today → UTC
            var yesterdayStartUtc = VietnamTime.DayStartUtc(todayVn.AddDays(-1));
            var endUtc = VietnamTime.DayEndExclusiveUtc(todayVn);

            var orderQueryable = await _orders.GetQueryableAsync();

            // perf-hunt 2026-10-19: 4 aggregate query quét cùng khoảng 2 ngày
            // (dashboard admin poll mỗi 30s) gộp thành 1 câu SQL duy nhất bằng
            // SUM/COUNT có điều kiện — cùng bộ lọc (loại đơn hủy, biên ngày VN)
            // nên kết quả giống hệt bản cũ. SUM có điều kiện trả NULL khi nhóm
            // rỗng nên project decimal? rồi ?? 0; không có dòng nào trong khoảng
            // → GroupBy không sinh nhóm → FirstOrDefaultAsync trả null → 0 hết.
            var agg = await _asyncExecuter.FirstOrDefaultAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= yesterdayStartUtc
                        && x.CreatedAt < endUtc)
                    .GroupBy(x => 1)
                    .Select(g => new TodayVsYesterdayAggregate
                    {
                        TodayRevenue = g.Where(x => x.CreatedAt >= startUtc)
                            .Sum(x => (decimal?)x.TotalAmount.Amount),
                        YesterdayRevenue = g.Where(x => x.CreatedAt < startUtc)
                            .Sum(x => (decimal?)x.TotalAmount.Amount),
                        TodayOrders = g.LongCount(x => x.CreatedAt >= startUtc),
                        YesterdayOrders = g.LongCount(x => x.CreatedAt < startUtc)
                    }));

            var todayRevenue = agg?.TodayRevenue ?? 0m;
            var yesterdayRevenue = agg?.YesterdayRevenue ?? 0m;
            var todayOrders = agg?.TodayOrders ?? 0;
            var yesterdayOrders = agg?.YesterdayOrders ?? 0;

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
                return today > 0 ? PercentMultiplier : 0;
            }

            return (double)(today - yesterday) / (double)yesterday * PercentMultiplier;
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
            var vnDate = DateTime.UtcNow.AddHours(VietnamTime.UtcOffsetHours).Date;
            var cacheKey = $"dashboard:today-sold:{vnDate:yyyyMMdd}";
            if (_memoryCache.TryGetValue(cacheKey, out long cached))
            {
                return cached;
            }

            // The shop runs on Vietnam time (UTC+7); CreatedAt is UTC.
            var startOfTodayUtc = VietnamTime.DayStartUtc(vnDate);

            var orderQueryable = await _orders.GetQueryableAsync();
            var billable = orderQueryable.Where(x =>
                x.Status != OrderStatus.Cancelled
                && x.CreatedAt >= startOfTodayUtc);

            var result = await _asyncExecuter.SumAsync(
                billable.SelectMany(x => x.Lines).Select(x => (long)x.Quantity));

            _memoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(TodaySoldCacheSeconds));
            return result;
        }
    }
}
