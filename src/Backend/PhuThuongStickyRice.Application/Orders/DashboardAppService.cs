using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using Volo.Abp.ObjectMapping;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IDashboardAppService : IApplicationService
    {
        Task<DashboardStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null);

        Task<List<TopProductDto>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5);

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
    }

    public class DashboardAppService : ApplicationService, IDashboardAppService
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;

        public DashboardAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
        }

        public async Task<DashboardStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null)
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

            return stats;
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5)
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

        public async Task<List<Guid>> GetBestSellerProductIdsAsync(int top)
        {
            var products = await GetTopProductsAsync(null, null, top < 1 ? 100 : top);
            return products.Select(x => x.ProductId).ToList();
        }

        public async Task<long> GetTodaySoldCountAsync()
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
