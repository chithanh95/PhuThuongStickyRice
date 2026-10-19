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

        /// <summary>Số khung giờ cao điểm hiển thị trong báo cáo.</summary>
        private const int TopHoursCount = 3;

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
            var day = (date?.Date ?? VietnamTime.Today());
            var fromUtc = VietnamTime.DayStartUtc(day);
            var toUtc = VietnamTime.DayEndExclusiveUtc(day);

            var buckets = await AggregatePerHourAsync(fromUtc, toUtc);

            return BuildReport(day, buckets);
        }

        /// <summary>
        /// Gom đơn của ngày theo (giờ VN, tên món): tổng số lượng từng món
        /// trong từng giờ, số đơn từng giờ và tổng số đơn trong ngày.
        /// </summary>
        private sealed class HourlyBuckets
        {
            public Dictionary<int, Dictionary<string, long>> QuantitiesPerHour { get; } = new();

            public Dictionary<int, long> OrdersPerHour { get; } = new();

            public long TotalOrders { get; set; }
        }

        private async Task<HourlyBuckets> AggregatePerHourAsync(DateTime fromUtc, DateTime toUtc)
        {
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

            var buckets = new HourlyBuckets();
            foreach (var o in raw)
            {
                var hour = VietnamTime.ToVietnamTime(o.CreatedAt).Hour;
                if (hour < DayStartHour || hour > DayEndHour)
                {
                    continue;
                }

                buckets.TotalOrders++;
                if (!buckets.QuantitiesPerHour.TryGetValue(hour, out var products))
                {
                    products = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    buckets.QuantitiesPerHour[hour] = products;
                    buckets.OrdersPerHour[hour] = 0;
                }

                buckets.OrdersPerHour[hour]++;
                foreach (var l in o.Lines)
                {
                    var name = string.IsNullOrWhiteSpace(l.ProductName) ? "(không tên)" : l.ProductName.Trim();
                    products.TryGetValue(name, out var qty);
                    products[name] = qty + l.Quantity;
                }
            }

            return buckets;
        }

        /// <summary>
        /// Dựng DTO báo cáo từ dữ liệu đã gom: danh sách món (sắp xếp theo
        /// tổng số lượng), ma trận giờ×món, top khung giờ và gợi ý chuẩn bị.
        /// </summary>
        private static HourlySalesReportDto BuildReport(DateTime day, HourlyBuckets buckets)
        {
            var productsOrdered = buckets.QuantitiesPerHour.Values
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
                buckets.QuantitiesPerHour.TryGetValue(h, out var products);
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
                .Take(TopHoursCount)
                .Select(r => new TopHourDto
                {
                    Hour = r.Hour,
                    Portions = r.Total,
                    Orders = buckets.OrdersPerHour.TryGetValue(r.Hour, out var c) ? c : 0
                })
                .ToList();

            var totalPortions = hours.Sum(r => r.Total);

            return new HourlySalesReportDto
            {
                Date = day,
                Products = productsOrdered,
                Hours = hours,
                TopHours = topHours,
                TotalOrders = buckets.TotalOrders,
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
