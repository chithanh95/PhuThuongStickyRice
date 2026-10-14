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
        Task<DashboardStatsDto> GetStatsAsync();
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

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var orderQueryable = await _orders.GetQueryableAsync();
            var productQueryable = await _products.GetQueryableAsync();
            var billable = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled);

            var stats = new DashboardStatsDto
            {
                TotalOrders = await _asyncExecuter.LongCountAsync(orderQueryable),
                PendingOrders = await _asyncExecuter.LongCountAsync(
                    orderQueryable.Where(x => x.Status == OrderStatus.Pending)),
                TotalRevenue = await _asyncExecuter.SumAsync(billable.Select(x => x.TotalAmount.Amount)),
                TotalProductsSold = await _asyncExecuter.SumAsync(
                    billable.SelectMany(x => x.Lines).Select(x => (long)x.Quantity)),
                TotalProducts = await _asyncExecuter.LongCountAsync(productQueryable)
            };

            stats.OrdersByStatus = await _asyncExecuter.ToListAsync(
                orderQueryable.GroupBy(x => x.Status)
                    .Select(g => new OrdersByStatusDto
                    {
                        Status = g.Key.ToString(),
                        Count = g.LongCount()
                    }));

            var weekAgo = DateTime.UtcNow.Date.AddDays(-6);
            stats.RevenueByDay = await _asyncExecuter.ToListAsync(
                billable.Where(x => x.CreatedAt >= weekAgo)
                    .GroupBy(x => x.CreatedAt.Date)
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
    }
}
