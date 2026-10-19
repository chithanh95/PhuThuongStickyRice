
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Reports;

namespace PhuThuongStickyRice.Application.Reports
{
    /// <summary>
    /// Item 84: profit report — revenue − cost = gross profit, by day and per
    /// product. Cost is valued at the product's current <c>CostPrice</c>
    /// (caveat: cost-price changes apply retroactively in this report;
    /// combos have no product row and count as zero cost).
    /// </summary>
    public interface IProfitReportAppService : IApplicationService
    {
        Task<ProfitReportDto> GetReportAsync(DateTime? from = null, DateTime? to = null);
    }

    [Authorize(Roles = "Admin")]
    public class ProfitReportAppService : ApplicationService, IProfitReportAppService
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public ProfitReportAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<ProfitReportDto> GetReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var (fromDate, toDate) = NormalizeRange(from, to);

            var orderQueryable = await _orders.GetQueryableAsync();
            var rangeOrders = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled
                && x.CreatedAt >= fromDate && x.CreatedAt < toDate);

            var productQueryable = await _products.GetQueryableAsync();

            var orderCounts = await QueryOrderCountsAsync(rangeOrders);
            var lineGroups = await QueryLineGroupsAsync(rangeOrders, productQueryable);

            var revenueCostByDay = lineGroups
                .GroupBy(x => x.Day)
                .ToDictionary(
                    g => g.Key,
                    g => (Revenue: g.Sum(x => x.Revenue), Cost: g.Sum(x => x.Cost)));

            var byDay = BuildDayDtos(orderCounts, revenueCostByDay);
            var byProduct = BuildProductDtos(lineGroups);

            return new ProfitReportDto
            {
                ByDay = byDay,
                ByProduct = byProduct,
                TotalRevenue = byDay.Sum(x => x.Revenue),
                TotalCost = byDay.Sum(x => x.Cost),
                TotalProfit = byDay.Sum(x => x.Profit)
            };
        }

        /// <summary>Số đơn theo từng ngày, tính trong SQL (kể cả đơn không có dòng món).</summary>
        private sealed class DayOrderCount
        {
            public DateTime Day { get; set; }
            public long Orders { get; set; }
        }

        /// <summary>Tổng hợp dòng món theo (ngày, món), tính trong SQL.</summary>
        private sealed class DayProductLine
        {
            public DateTime Day { get; set; }
            public Guid ProductId { get; set; }
            public string ProductName { get; set; }
            public long Quantity { get; set; }
            public decimal Revenue { get; set; }
            public decimal Cost { get; set; }
        }

        // Perf: tổng hợp trong SQL thay vì nạp toàn bộ order + line vào bộ nhớ
        // (khoảng ngày không giới hạn — admin có thể yêu cầu cả năm).
        // Giá vốn dùng giá HIỆN TẠI của từng món (tính hồi tố, giống lookup
        // costMap trước đây).

        /// <summary>Đếm số đơn theo từng ngày ngay trong SQL.</summary>
        private async Task<List<DayOrderCount>> QueryOrderCountsAsync(IQueryable<Order> rangeOrders)
        {
            return await _asyncExecuter.ToListAsync(
                rangeOrders
                    .GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new DayOrderCount { Day = g.Key, Orders = g.LongCount() }));
        }

        /// <summary>
        /// Tổng hợp doanh thu/giá vốn theo (ngày, món) ngay trong SQL, join
        /// với giá vốn hiện tại của từng món. Dòng nào không có product row
        /// (combo) join NULL → giá vốn bằng 0, giống fallback
        /// costMap.TryGetValue trước đây.
        /// </summary>
        private async Task<List<DayProductLine>> QueryLineGroupsAsync(
            IQueryable<Order> rangeOrders, IQueryable<Product> productQueryable)
        {
            return await _asyncExecuter.ToListAsync(
                from o in rangeOrders
                from l in o.Lines
                join p in productQueryable on l.ProductId equals p.Id into ps
                from p in ps.DefaultIfEmpty()
                group new
                {
                    Quantity = (long)l.Quantity,
                    Revenue = l.LineTotal.Amount,
                    Cost = (long)l.Quantity * (p == null ? 0m : p.CostPrice),
                    ProductName = l.ProductName
                }
                by new { Day = o.CreatedAt.Date, l.ProductId, l.ProductName } into g
                select new DayProductLine
                {
                    Day = g.Key.Day,
                    ProductId = g.Key.ProductId,
                    ProductName = g.Max(x => x.ProductName),
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost)
                });
        }

        /// <summary>Dựng DTO từng ngày: ghép số đơn với doanh thu/giá vốn của ngày đó.</summary>
        private static List<ProfitDayDto> BuildDayDtos(
            List<DayOrderCount> orderCounts,
            Dictionary<DateTime, (decimal Revenue, decimal Cost)> revenueCostByDay)
        {
            return orderCounts
                .OrderBy(x => x.Day)
                .Select(x =>
                {
                    revenueCostByDay.TryGetValue(x.Day, out var rc);
                    return new ProfitDayDto
                    {
                        Date = x.Day,
                        Revenue = rc.Revenue,
                        Cost = rc.Cost,
                        Profit = rc.Revenue - rc.Cost,
                        Orders = x.Orders
                    };
                })
                .ToList();
        }

        /// <summary>Dựng DTO từng món: gom các dòng (ngày, món) theo món, sắp xếp theo doanh thu giảm dần.</summary>
        private static List<ProfitProductDto> BuildProductDtos(List<DayProductLine> lineGroups)
        {
            return lineGroups
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new ProfitProductDto
                {
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost),
                    Profit = g.Sum(x => x.Revenue) - g.Sum(x => x.Cost)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();
        }

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
