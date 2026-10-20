// CS8632: file dùng annotation nullable (?); bật ngữ cảnh annotations để
// compiler hiểu đúng ý nghĩa, không bật phân tích luồng (dự án chưa bật nullable toàn cục).
#nullable enable annotations

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
    /// <summary>
    /// Hợp đồng báo cáo doanh thu theo giờ (admin): gom đơn trong ngày theo
    /// khung giờ VN, kèm ma trận giờ×món và gợi ý chuẩn bị nguyên liệu cho bếp.
    /// </summary>
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
        /// <summary>Giờ mở đầu khung báo cáo (quán bán từ 6h sáng; đơn ngoài khung không đáng kể).</summary>
        private const int DayStartHour = 6;
        /// <summary>Giờ kết thúc khung báo cáo (quán nghỉ sau 22h).</summary>
        private const int DayEndHour = 22;

        /// <summary>Số khung giờ cao điểm hiển thị trong báo cáo.</summary>
        private const int TopHoursCount = 3;

        /// <summary>Nhãn hiển thị cho món không có tên (giữ đồng bộ với các báo cáo khác).</summary>
        private const string UnnamedProductLabel = "(không tên)";

        private readonly IRepository<Order, Guid> _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public OrderHourlyReportAppService(
            IRepository<Order, Guid> orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Admin: báo cáo món bán chạy theo từng giờ của 1 ngày (giờ Việt Nam).
        /// Mặc định lấy hôm nay. Đơn ngoài khung 6h–22h bị bỏ qua vì quán không
        /// bán trong khung đó.
        /// </summary>
        /// <param name="date">Ngày cần xem (mặc định hôm nay, giờ Việt Nam).</param>
        /// <returns>Ma trận giờ×món, top khung giờ đông khách và gợi ý chuẩn bị.</returns>
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

        /// <summary>
        /// Gom dữ liệu 1 ngày bằng 2 câu truy vấn SQL: (1) số lượng từng món
        /// theo (giờ VN, tên món), (2) số đơn theo giờ VN. Tách 2 câu vì 2 cấp
        /// nhóm dùng aggregate khác nhau; đơn đã hủy bị loại ngay trong SQL
        /// để ngày đông đơn không nạp cả đơn + lines vào memory.
        /// </summary>
        /// <param name="fromUtc">Đầu ngày VN (đã quy về UTC).</param>
        /// <param name="toUtc">Cuối ngày VN (exclusive, đã quy về UTC).</param>
        /// <returns>Buckets dữ liệu thô để <see cref="BuildReport"/> dựng DTO.</returns>
        private async Task<HourlyBuckets> AggregatePerHourAsync(DateTime fromUtc, DateTime toUtc)
        {
            var queryable = await _orders.GetQueryableAsync();
            var baseQuery = queryable
                .Where(x => x.CreatedAt >= fromUtc
                    && x.CreatedAt < toUtc
                    && x.Status != OrderStatus.Cancelled);

            // perf-hunt 2026-10-19: gom ngay trong SQL theo (giờ VN, tên món)
            // thay vì nạp toàn bộ đơn + lines của cả ngày vào memory rồi gom
            // bằng C#. VN không có DST nên AddHours(offset) dịch được sang SQL.
            // Tên món vẫn chuẩn hóa ở C# (trim/UnnamedProductLabel) nên semantics giữ nguyên.
            // CS8143: tuple literal không dùng được trong expression tree —
            // project anonymous type trong SQL, map sang tuple ở memory.
            var quantityRows = (await _asyncExecuter.ToListAsync(
                baseQuery
                    .SelectMany(x => x.Lines.Select(l => new
                    {
                        Hour = x.CreatedAt.AddHours(VietnamTime.UtcOffsetHours).Hour,
                        l.ProductName,
                        l.Quantity
                    }))
                    .GroupBy(x => new { x.Hour, x.ProductName })
                    .Select(g => new
                    {
                        g.Key.Hour,
                        g.Key.ProductName,
                        Quantity = g.Sum(x => (long)x.Quantity)
                    })))
                .Select(x => (x.Hour, x.ProductName, x.Quantity))
                .ToList();

            // 1 câu SQL đếm số đơn theo giờ VN (OrdersPerHour/TotalOrders).
            var orderCountRows = (await _asyncExecuter.ToListAsync(
                baseQuery
                    .GroupBy(x => x.CreatedAt.AddHours(VietnamTime.UtcOffsetHours).Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        Count = g.LongCount()
                    })))
                .Select(x => (x.Hour, x.Count))
                .ToList();

            var buckets = new HourlyBuckets();
            FillProductQuantities(buckets, quantityRows);
            FillOrderCounts(buckets, orderCountRows);

            return buckets;
        }

        /// <summary>
        /// Đổ số lượng từng món theo giờ vào buckets (chuẩn hóa tên món ở C#
        /// như bản cũ; đơn ngoài khung 6h–22h bị bỏ qua).
        /// </summary>
        private static void FillProductQuantities(
            HourlyBuckets buckets,
            IEnumerable<(int Hour, string? ProductName, long Quantity)> rows)
        {
            foreach (var row in rows)
            {
                if (!IsWithinReportingHours(row.Hour))
                {
                    continue;
                }

                if (!buckets.QuantitiesPerHour.TryGetValue(row.Hour, out var products))
                {
                    products = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                    buckets.QuantitiesPerHour[row.Hour] = products;
                    buckets.OrdersPerHour[row.Hour] = 0;
                }

                // Chuẩn hóa tên y hệt bản cũ (để SQL gom theo raw name không ảnh hưởng kết quả).
                var name = string.IsNullOrWhiteSpace(row.ProductName) ? UnnamedProductLabel : row.ProductName.Trim();
                products.TryGetValue(name, out var qty);
                products[name] = qty + row.Quantity;
            }
        }

        /// <summary>Đổ số đơn từng giờ vào buckets (đơn ngoài khung 6h–22h bị bỏ qua).</summary>
        private static void FillOrderCounts(
            HourlyBuckets buckets,
            IEnumerable<(int Hour, long Count)> rows)
        {
            foreach (var row in rows)
            {
                if (!IsWithinReportingHours(row.Hour))
                {
                    continue;
                }

                buckets.OrdersPerHour[row.Hour] = row.Count;
                buckets.TotalOrders += row.Count;
            }
        }

        /// <summary>
        /// Đơn trong khung <see cref="DayStartHour"/>h–<see cref="DayEndHour"/>h
        /// (giờ VN) mới tính vào báo cáo (quán không bán ngoài khung này).
        /// </summary>
        private static bool IsWithinReportingHours(int hour)
            => hour >= DayStartHour && hour <= DayEndHour;

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

            var hours = BuildHourlyRows(buckets, productsOrdered);
            var topHours = BuildTopHours(buckets, hours);
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

        /// <summary>
        /// Dựng ma trận giờ×món: mỗi hàng là 1 giờ trong khung báo cáo, mỗi
        /// cột là số lượng bán của 1 món (0 khi giờ đó không bán món ấy).
        /// </summary>
        private static List<HourlySalesRowDto> BuildHourlyRows(HourlyBuckets buckets, List<string> productsOrdered)
        {
            var hours = new List<HourlySalesRowDto>();
            for (var hour = DayStartHour; hour <= DayEndHour; hour++)
            {
                buckets.QuantitiesPerHour.TryGetValue(hour, out var products);
                var quantities = productsOrdered
                    .Select(p => products != null && products.TryGetValue(p, out var quantity) ? quantity : 0L)
                    .ToList();
                hours.Add(new HourlySalesRowDto
                {
                    Hour = hour,
                    Quantities = quantities,
                    Total = quantities.Sum()
                });
            }

            return hours;
        }

        /// <summary>
        /// Chọn các khung giờ bán chạy nhất (có đơn) để admin biết giờ cao
        /// điểm chuẩn bị hàng.
        /// </summary>
        private static List<TopHourDto> BuildTopHours(HourlyBuckets buckets, List<HourlySalesRowDto> hours)
        {
            return hours
                .Where(r => r.Total > 0)
                .OrderByDescending(r => r.Total)
                .Take(TopHoursCount)
                .Select(r => new TopHourDto
                {
                    Hour = r.Hour,
                    Portions = r.Total,
                    Orders = buckets.OrdersPerHour.TryGetValue(r.Hour, out var orderCount) ? orderCount : 0
                })
                .ToList();
        }

        /// <summary>
        /// Gợi ý chuẩn bị hàng từ dữ liệu: gọi tên khung giờ đông nhất để bếp
        /// nấu trước, kèm top 3 khung giờ. Ngày không có dữ liệu thì trả câu
        /// thông báo thay vì chuỗi rỗng.
        /// </summary>
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
