using PhuThuongStickyRice.Application.Wave3.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Wave3
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

            var productQueryable = await _products.GetQueryableAsync();
            var costRows = await _asyncExecuter.ToListAsync(
                productQueryable.Select(x => new { x.Id, x.CostPrice }));
            var costMap = costRows.ToDictionary(x => x.Id, x => x.CostPrice);

            var orderQueryable = await _orders.GetQueryableAsync();
            var orders = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= fromDate && x.CreatedAt < toDate)
                    .Select(x => new
                    {
                        Day = x.CreatedAt.Date,
                        Lines = x.Lines.Select(l => new
                        {
                            l.ProductId,
                            l.ProductName,
                            l.Quantity,
                            LineTotal = l.LineTotal.Amount
                        }).ToList()
                    }));

            decimal CostOf(Guid productId, int quantity) =>
                quantity * (costMap.TryGetValue(productId, out var c) ? c : 0);

            var byDay = orders
                .GroupBy(x => x.Day)
                .Select(g =>
                {
                    var lines = g.SelectMany(x => x.Lines).ToList();
                    var revenue = lines.Sum(l => l.LineTotal);
                    var cost = lines.Sum(l => CostOf(l.ProductId, l.Quantity));
                    return new ProfitDayDto
                    {
                        Date = g.Key,
                        Revenue = revenue,
                        Cost = cost,
                        Profit = revenue - cost,
                        Orders = g.LongCount()
                    };
                })
                .OrderBy(x => x.Date)
                .ToList();

            var byProduct = orders
                .SelectMany(x => x.Lines)
                .GroupBy(l => new { l.ProductId, l.ProductName })
                .Select(g =>
                {
                    var revenue = g.Sum(l => l.LineTotal);
                    var qty = g.Sum(l => (long)l.Quantity);
                    var cost = g.Sum(l => CostOf(l.ProductId, l.Quantity));
                    return new ProfitProductDto
                    {
                        ProductName = g.Key.ProductName,
                        QuantitySold = qty,
                        Revenue = revenue,
                        Cost = cost,
                        Profit = revenue - cost
                    };
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
