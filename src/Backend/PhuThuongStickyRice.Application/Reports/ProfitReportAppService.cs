
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

            // Perf: aggregate in SQL instead of materializing every order +
            // line in range (range is unbounded — admin can request a full year).
            // Same numbers as before; cost uses each product's CURRENT cost
            // price (retroactive, like the old costMap lookup).

            // Orders per day (SQL) — includes orders with no lines, like before.
            var orderCounts = await _asyncExecuter.ToListAsync(
                rangeOrders
                    .GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new { Day = g.Key, Orders = g.LongCount() }));

            // Line aggregates, joined to current cost prices (SQL). Lines whose
            // id has no product row (combos) join NULL → zero cost, same as
            // the old costMap.TryGetValue fallback.
            var lineGroups = await _asyncExecuter.ToListAsync(
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
                select new
                {
                    Day = g.Key.Day,
                    ProductId = g.Key.ProductId,
                    ProductName = g.Max(x => x.ProductName),
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Revenue),
                    Cost = g.Sum(x => x.Cost)
                });

            var revenueCostByDay = lineGroups
                .GroupBy(x => x.Day)
                .ToDictionary(
                    g => g.Key,
                    g => (Revenue: g.Sum(x => x.Revenue), Cost: g.Sum(x => x.Cost)));

            var byDay = orderCounts
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

            var byProduct = lineGroups
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

            return new ProfitReportDto
            {
                ByDay = byDay,
                ByProduct = byProduct,
                TotalRevenue = byDay.Sum(x => x.Revenue),
                TotalCost = byDay.Sum(x => x.Cost),
                TotalProfit = byDay.Sum(x => x.Profit)
            };
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
