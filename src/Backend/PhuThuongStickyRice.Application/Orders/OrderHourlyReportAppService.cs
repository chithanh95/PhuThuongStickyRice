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

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IOrderHourlyReportAppService : IApplicationService
    {
        /// <summary>
        /// Admin: per-hour best-sellers report for one Vietnam-local day.
        /// </summary>
        Task<HourlySalesReportDto> GetHourlySalesReportAsync(DateTime? date);
    }

    /// <summary>
    /// Hourly sales report. Read-only, no schema changes:
    /// aggregates non-cancelled orders of the requested Vietnam day,
    /// grouping order lines by (VN hour, product name).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class OrderHourlyReportAppService : ApplicationService, IOrderHourlyReportAppService
    {
        private const int DayStartHour = 6;
        private const int DayEndHour = 22;

        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public OrderHourlyReportAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<HourlySalesReportDto> GetHourlySalesReportAsync(DateTime? date)
        {
            var day = (date?.Date ?? VietnamTime.Today);
            var fromUtc = VietnamTime.DayStartUtc(day);
            var toUtc = VietnamTime.DayEndExclusiveUtc(day);

            var queryable = await _orders.GetQueryableAsync();
            var raw = await _asyncExecuter.ToListAsync(
                queryable
                    .Where(x => x.CreatedAt >= fromUtc
                        && x.CreatedAt < toUtc
                        && x.Status != OrderStatus.Cancelled)
                    .Select(x => new
                    {
                        x.CreatedAt,
                        Lines = x.Lines.Select(l => new
                        {
                            l.ProductName,
                            l.Quantity
                        }).ToList()
                    }));

            // Aggregate per Vietnam-local hour and product.
            var perHour = new Dictionary<int, Dictionary<string, long>>();
            var perHourOrders = new Dictionary<int, long>();
            long totalOrders = 0;
            foreach (var o in raw)
            {
                var hour = o.CreatedAt.AddHours(7).Hour;
                if (hour < DayStartHour || hour > DayEndHour)
                {
                    continue;
                }

                totalOrders++;
                if (!perHour.TryGetValue(hour, out var products))
                {
                    products = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    perHour[hour] = products;
                    perHourOrders[hour] = 0;
                }

                perHourOrders[hour]++;
                foreach (var l in o.Lines)
                {
                    var name = string.IsNullOrWhiteSpace(l.ProductName) ? "(không tên)" : l.ProductName.Trim();
                    products.TryGetValue(name, out var qty);
                    products[name] = qty + l.Quantity;
                }
            }

            var productsOrdered = perHour.Values
                .SelectMany(d => d)
                .GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Name = g.Key, Qty = g.Sum(kv => kv.Value) })
                .OrderByDescending(x => x.Qty)
                .ThenBy(x => x.Name)
                .Select(x => x.Name)
                .ToList();

            var hours = new List<HourlySalesRowDto>();
            for (var h = DayStartHour; h <= DayEndHour; h++)
            {
                perHour.TryGetValue(h, out var products);
                var quantities = productsOrdered
                    .Select(p => products != null && products.TryGetValue(p, out var q) ? q : 0L)
                    .ToList();
                hours.Add(new HourlySalesRowDto
                {
                    Hour = h,
                    Quantities = quantities,
                    Total = quantities.Sum()
                });
            }

            var topHours = hours
                .Where(r => r.Total > 0)
                .OrderByDescending(r => r.Total)
                .Take(3)
                .Select(r => new TopHourDto
                {
                    Hour = r.Hour,
                    Portions = r.Total,
                    Orders = perHourOrders.TryGetValue(r.Hour, out var c) ? c : 0
                })
                .ToList();

            var totalPortions = hours.Sum(r => r.Total);

            return new HourlySalesReportDto
            {
                Date = day,
                Products = productsOrdered,
                Hours = hours,
                TopHours = topHours,
                TotalOrders = totalOrders,
                TotalPortions = totalPortions,
                Suggestion = BuildSuggestion(day, topHours, totalPortions)
            };
        }

        private static string BuildSuggestion(DateTime day, List<TopHourDto> topHours, long totalPortions)
        {
            if (topHours.Count == 0 || totalPortions == 0)
            {
                return $"Ngày {day:dd/MM/yyyy} chưa có dữ liệu bán hàng.";
            }

            var peak = topHours[0];
            var ranges = string.Join(", ", topHours.Select(t => $"{t.Hour}h–{t.Hour + 1}h ({t.Portions} suất)"));
            return $"Khung giờ đông nhất: {peak.Hour}h–{peak.Hour + 1}h ({peak.Portions} suất). " +
                   $"Nên chuẩn bị nhiều xôi trước {peak.Hour}h. " +
                   $"Top 3 khung giờ: {ranges}.";
        }
    }
}
