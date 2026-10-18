using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IGrossProfitReportAppService : IApplicationService
    {
        /// <summary>
        /// Admin: gross profit per product for a Vietnam date range
        /// (defaults to the last 7 days, inclusive).
        /// </summary>
        Task<GrossProfitReportDto> GetGrossProfitReportAsync(DateTime? from, DateTime? to);

        /// <summary>Admin: all products with their current cost price.</summary>
        Task<List<ProductCostDto>> GetProductCostsAsync();

        /// <summary>Admin: sets the cost price (giá vốn) of one product.</summary>
        Task<ProductCostDto> UpdateCostPriceAsync(Guid productId, decimal costPrice);
    }

    /// <summary>
    /// Item 154: gross profit report. Read-only aggregation over the lines
    /// of non-cancelled orders; cost is valued at each product's CURRENT
    /// <see cref="Product.CostPrice"/> — the system keeps no cost history,
    /// so editing a cost price changes past periods too (the UI states this).
    /// Combos have no product row and count as zero cost.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class GrossProfitReportAppService : ApplicationService, IGrossProfitReportAppService
    {
        private readonly IRepository<Order, Guid> _orders;
        private readonly IRepository<Product, Guid> _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public GrossProfitReportAppService(
            IRepository<Order, Guid> orders,
            IRepository<Product, Guid> products,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<GrossProfitReportDto> GetGrossProfitReportAsync(DateTime? from, DateTime? to)
        {
            var today = VietnamTime.Today();
            var toDate = (to?.Date ?? today).Date;
            var fromDate = (from?.Date ?? toDate.AddDays(-6)).Date;
            if (fromDate > toDate)
            {
                fromDate = toDate.AddDays(-6);
            }

            var fromUtc = VietnamTime.DayStartUtc(fromDate);
            var toUtc = VietnamTime.DayEndExclusiveUtc(toDate);

            var productQueryable = await _products.GetQueryableAsync();
            var costRows = await _asyncExecuter.ToListAsync(
                productQueryable.Select(x => new { x.Id, x.CostPrice }));
            var costMap = costRows.ToDictionary(x => x.Id, x => x.CostPrice);

            var orderQueryable = await _orders.GetQueryableAsync();
            var orders = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= fromUtc
                        && x.CreatedAt < toUtc)
                    .Select(x => new
                    {
                        Lines = x.Lines.Select(l => new
                        {
                            l.ProductId,
                            l.ProductName,
                            l.Quantity,
                            LineTotal = l.LineTotal.Amount
                        }).ToList()
                    }));

            decimal CostOf(Guid productId, int quantity) =>
                quantity * (costMap.TryGetValue(productId, out var c) ? c : 0m);

            var rows = orders
                .SelectMany(x => x.Lines)
                .GroupBy(l => l.ProductId)
                .Select(g =>
                {
                    var first = g.First();
                    var qty = g.Sum(l => (long)l.Quantity);
                    var revenue = g.Sum(l => l.LineTotal);
                    var unitCost = costMap.TryGetValue(g.Key, out var c) ? c : 0m;
                    var cost = qty * unitCost;
                    var profit = revenue - cost;
                    return new GrossProfitRowDto
                    {
                        ProductId = g.Key,
                        ProductName = string.IsNullOrWhiteSpace(first.ProductName)
                            ? "(không tên)" : first.ProductName.Trim(),
                        QuantitySold = qty,
                        Revenue = revenue,
                        Cost = cost,
                        Profit = profit,
                        MarginPercent = revenue > 0
                            ? Math.Round(profit / revenue * 100m, 1)
                            : (decimal?)null,
                        CostPrice = unitCost,
                        CostMissing = unitCost <= 0m
                    };
                })
                .OrderByDescending(r => r.Profit)
                .ThenByDescending(r => r.Revenue)
                .ToList();

            var totalQty = rows.Sum(r => r.QuantitySold);
            var totalRevenue = rows.Sum(r => r.Revenue);
            var totalCost = rows.Sum(r => r.Cost);
            var totalProfit = rows.Sum(r => r.Profit);

            return new GrossProfitReportDto
            {
                From = fromDate,
                To = toDate,
                Rows = rows,
                Total = new GrossProfitTotalDto
                {
                    QuantitySold = totalQty,
                    Revenue = totalRevenue,
                    Cost = totalCost,
                    Profit = totalProfit,
                    MarginPercent = totalRevenue > 0
                        ? Math.Round(totalProfit / totalRevenue * 100m, 1)
                        : (decimal?)null
                },
                Suggestions = BuildSuggestions(rows),
                HasUnpricedProducts = rows.Any(r => r.CostMissing),
                CostBasisNote = "Giá vốn tính theo giá hiện tại của từng món. " +
                    "Hệ thống không lưu lịch sử thay đổi giá vốn, nên nếu bạn sửa giá vốn, " +
                    "số liệu lãi của các kỳ trước cũng sẽ thay đổi theo."
            };
        }

        public virtual async Task<List<ProductCostDto>> GetProductCostsAsync()
        {
            var productQueryable = await _products.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(
                productQueryable
                    .OrderBy(x => x.Name)
                    .Select(x => new ProductCostDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Unit = x.Unit,
                        Price = x.Price.Amount,
                        CostPrice = x.CostPrice
                    }));
        }

        public virtual async Task<ProductCostDto> UpdateCostPriceAsync(Guid productId, decimal costPrice)
        {
            var product = await _products.GetAsync(productId);
            product.ChangeCostPrice(costPrice);
            await _products.UpdateAsync(product);

            return new ProductCostDto
            {
                Id = product.Id,
                Name = product.Name,
                Unit = product.Unit,
                Price = product.Price.Amount,
                CostPrice = product.CostPrice
            };
        }

        private static List<string> BuildSuggestions(List<GrossProfitRowDto> rows)
        {
            var suggestions = new List<string>();

            if (rows.Count == 0)
            {
                suggestions.Add("Khoảng thời gian này chưa có đơn bán nào.");
                return suggestions;
            }

            var unpriced = rows.Where(r => r.CostMissing).ToList();
            if (unpriced.Count > 0)
            {
                suggestions.Add(
                    $"Có {unpriced.Count} món chưa nhập giá vốn " +
                    $"({string.Join(", ", unpriced.Take(3).Select(r => r.ProductName))}" +
                    $"{(unpriced.Count > 3 ? ",…" : "")}) — lãi các món này đang tính bằng 100% doanh thu. " +
                    "Nhập giá vốn ở bảng bên dưới để số liệu chính xác.");
            }

            var priced = rows.Where(r => !r.CostMissing && r.Revenue > 0).ToList();
            if (priced.Count > 0)
            {
                var medianQty = Median(priced.Select(r => (double)r.QuantitySold).ToList());
                var highMarginLowVolume = priced
                    .Where(r => r.QuantitySold <= medianQty && r.MarginPercent.HasValue)
                    .OrderByDescending(r => r.MarginPercent)
                    .FirstOrDefault();
                if (highMarginLowVolume != null)
                {
                    suggestions.Add(
                        $"Món \"{highMarginLowVolume.ProductName}\" có biên lãi cao nhất " +
                        $"({highMarginLowVolume.MarginPercent:0.#}%) nhưng mới bán {highMarginLowVolume.QuantitySold:N0} suất — " +
                        "nên đẩy bán (đưa lên đầu trang shop, gợi ý kèm khi khách đặt).");
                }

                var lowestMargin = priced
                    .Where(r => r.MarginPercent.HasValue)
                    .OrderBy(r => r.MarginPercent)
                    .FirstOrDefault();
                if (lowestMargin != null && lowestMargin.MarginPercent < 30m
                    && (highMarginLowVolume == null || lowestMargin.ProductId != highMarginLowVolume.ProductId))
                {
                    suggestions.Add(
                        $"Món \"{lowestMargin.ProductName}\" biên lãi thấp ({lowestMargin.MarginPercent:0.#}%) — " +
                        "cân nhắc tăng giá bán hoặc tìm nguồn nguyên liệu rẻ hơn.");
                }
            }

            var topProfit = rows.FirstOrDefault();
            if (topProfit != null && topProfit.Profit > 0)
            {
                suggestions.Add(
                    $"Món đóng góp lãi nhiều nhất: \"{topProfit.ProductName}\" " +
                    $"({topProfit.Profit:N0}₫ lãi gộp, {topProfit.QuantitySold:N0} suất).");
            }

            return suggestions;
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }
    }
}
