using MediatR;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Orders.Queries
{
    public sealed class GetDashboardStatsQuery : IRequest<DashboardStatsDto>
    {
    }

    internal sealed class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;

        public GetDashboardStatsQueryHandler(IOrderRepository orders, IProductRepository products)
        {
            _orders = orders;
            _products = products;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var orders = _orders.GetAll();
            var billable = orders.Where(x => x.Status != OrderStatus.Cancelled);

            var stats = new DashboardStatsDto
            {
                TotalOrders = await _orders.CountAsync(orders, cancellationToken),
                PendingOrders = await _orders.CountAsync(
                    orders.Where(x => x.Status == OrderStatus.Pending), cancellationToken),
                TotalRevenue = await SumAsync(billable.Select(x => x.TotalAmount.Amount), cancellationToken),
                TotalProductsSold = await SumLongAsync(
                    billable.SelectMany(x => x.Lines).Select(x => (long)x.Quantity), cancellationToken),
                TotalProducts = await _products.CountAsync(_products.GetAll(), cancellationToken)
            };

            var byStatus = await _orders.ToListAsync(
                orders.GroupBy(x => x.Status)
                    .Select(g => new OrdersByStatusDto
                    {
                        Status = g.Key.ToString(),
                        Count = g.LongCount()
                    }));
            stats.OrdersByStatus = byStatus;

            var weekAgo = DateTime.UtcNow.Date.AddDays(-6);
            var revenueByDay = await _orders.ToListAsync(
                billable.Where(x => x.CreatedAt >= weekAgo)
                    .GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new RevenueByDayDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(x => x.TotalAmount.Amount),
                        Orders = g.LongCount()
                    })
                    .OrderBy(x => x.Date));
            stats.RevenueByDay = revenueByDay;

            var recent = await _orders.ToListAsync(orders.OrderByDescending(x => x.CreatedAt).Take(5));
            stats.RecentOrders = new List<OrderDto>();
            foreach (var order in recent)
            {
                stats.RecentOrders.Add(order.ToDto());
            }

            return stats;
        }

        private Task<decimal> SumAsync(IQueryable<decimal> query, CancellationToken cancellationToken)
        {
            return _orders.FirstOrDefaultAsync(
                query.GroupBy(x => 1).Select(g => (decimal?)g.Sum() ?? 0m));
        }

        private Task<long> SumLongAsync(IQueryable<long> query, CancellationToken cancellationToken)
        {
            return _orders.FirstOrDefaultAsync(
                query.GroupBy(x => 1).Select(g => (long?)g.Sum() ?? 0L));
        }
    }
}
