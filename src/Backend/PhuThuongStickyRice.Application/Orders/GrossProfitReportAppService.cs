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
using PhuThuongStickyRice.Application.Products;

namespace PhuThuongStickyRice.Application.Orders
{
    /// <summary>
    /// Hợp đồng báo cáo lợi nhuận gộp (admin): lợi nhuận theo từng món trong
    /// khoảng ngày, xem/sửa giá vốn. Giá vốn tính theo giá hiện tại của món
    /// (hệ thống không lưu lịch sử giá vốn — sửa giá làm đổi cả kỳ cũ).
    /// </summary>
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
        private readonly IProductCatalogCache _catalogCache;

        public GrossProfitReportAppService(
            IRepository<Order, Guid> orders,
            IRepository<Product, Guid> products,
            IAsyncQueryableExecuter asyncExecuter,
            IProductCatalogCache catalogCache)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _catalogCache = catalogCache;
        }

        // Báo cáo mặc định lùi về trước bao nhiêu ngày (6 = hôm nay + 6 ngày trước = 7 ngày).
        private const int DefaultReportLookbackDays = 6;
        // Biên lãi tính theo phần trăm, làm tròn đến 1 chữ số thập phân.
        private const decimal PercentFactor = 100m;
        private const int MarginRoundDecimals = 1;
        // Biên lãi dưới ngưỡng này được gợi ý "tăng giá / tìm nguyên liệu rẻ hơn".
        private const decimal LowMarginPercentThreshold = 30m;
        // Số tên món tối đa liệt kê trong gợi ý "chưa nhập giá vốn".
        private const int MaxUnpricedNamesInSuggestion = 3;
        // Nhãn hiển thị cho món không có tên (dòng legacy/combo thiếu tên món).
        private const string UnnamedProductLabel = "(không tên)";

        /// <summary>
        /// Admin: báo cáo lãi gộp theo món trong khoảng ngày (giờ VN,
        /// mặc định 7 ngày gần nhất). Đơn đã hủy bị loại. Giá vốn dùng giá
        /// HIỆN TẠI của từng món (tính hồi tố); combo không có product row
        /// tính giá vốn 0. Trả kèm gợi ý kinh doanh (món chưa nhập giá vốn,
        /// món biên lãi cao/thấp bất thường).
        /// </summary>
        /// <param name="from">Ngày bắt đầu (mặc định 7 ngày gần nhất).</param>
        /// <param name="to">Ngày kết thúc (mặc định hôm nay).</param>
        /// <returns>Chi tiết theo món, dòng tổng cộng, gợi ý và ghi chú giá vốn.</returns>
        public virtual async Task<GrossProfitReportDto> GetGrossProfitReportAsync(DateTime? from, DateTime? to)
        {
            var (fromDate, toDate) = ResolveReportDateRange(from, to);
            var fromUtc = VietnamTime.DayStartUtc(fromDate);
            var toUtc = VietnamTime.DayEndExclusiveUtc(toDate);

            var lineGroups = await QueryLineAggregatesAsync(fromUtc, toUtc);
            var rows = BuildRowDtos(lineGroups);

            return new GrossProfitReportDto
            {
                From = fromDate,
                To = toDate,
                Rows = rows,
                Total = BuildReportTotal(rows),
                Suggestions = BuildSuggestions(rows),
                HasUnpricedProducts = rows.Any(r => r.CostMissing),
                CostBasisNote = "Giá vốn tính theo giá hiện tại của từng món. " +
                    "Hệ thống không lưu lịch sử thay đổi giá vốn, nên nếu bạn sửa giá vốn, " +
                    "số liệu lãi của các kỳ trước cũng sẽ thay đổi theo."
            };
        }

        /// <summary>Kết quả tổng hợp doanh thu/số lượng/giá vốn theo từng món, tính ngay trong SQL.</summary>
        private sealed class LineAggregate
        {
            public Guid ProductId { get; set; }
            public string ProductName { get; set; }
            public long QuantitySold { get; set; }
            public decimal Revenue { get; set; }
            public decimal UnitCost { get; set; }
        }

        /// <summary>Chuẩn hóa khoảng ngày báo cáo: mặc định 7 ngày gần nhất (tính cả hôm nay).</summary>
        private static (DateTime FromDate, DateTime ToDate) ResolveReportDateRange(DateTime? from, DateTime? to)
        {
            var today = VietnamTime.Today();
            var toDate = (to?.Date ?? today).Date;
            var fromDate = (from?.Date ?? toDate.AddDays(-DefaultReportLookbackDays)).Date;
            if (fromDate > toDate)
            {
                fromDate = toDate.AddDays(-DefaultReportLookbackDays);
            }

            return (fromDate, toDate);
        }

        /// <summary>
        /// Tổng hợp doanh thu/số lượng theo từng món ngay trong SQL thay vì nạp
        /// toàn bộ order + line vào bộ nhớ (khoảng ngày không giới hạn — admin
        /// có thể yêu cầu cả năm). Giá vốn dùng giá HIỆN TẠI của từng món
        /// (tính hồi tố, giống lookup costMap trước đây). Dòng nào không có
        /// product row (combo) join NULL → giá vốn bằng 0.
        /// </summary>
        private async Task<List<LineAggregate>> QueryLineAggregatesAsync(DateTime fromUtc, DateTime toUtc)
        {
            var orderQueryable = await _orders.GetQueryableAsync();
            var rangeOrders = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled
                && x.CreatedAt >= fromUtc
                && x.CreatedAt < toUtc);

            // NOTE: ProductName lấy Max trong SQL cho xác định; code cũ lấy tên
            // dòng đầu theo thứ tự bất kỳ nên mọi lựa chọn xác định đều tương đương.
            var productQueryable = await _products.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(
                from o in rangeOrders
                from l in o.Lines
                join p in productQueryable on l.ProductId equals p.Id into ps
                from p in ps.DefaultIfEmpty()
                group new
                {
                    Quantity = (long)l.Quantity,
                    Revenue = l.LineTotal.Amount,
                    UnitCost = p == null ? 0m : p.CostPrice,
                    ProductName = l.ProductName
                }
                by l.ProductId into g
                select new LineAggregate
                {
                    ProductId = g.Key,
                    ProductName = g.Max(x => x.ProductName),
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Revenue),
                    UnitCost = g.Max(x => x.UnitCost)
                });
        }

        /// <summary>Ánh xạ kết quả tổng hợp sang DTO từng dòng, sắp xếp theo lãi giảm dần.</summary>
        private static List<GrossProfitRowDto> BuildRowDtos(List<LineAggregate> lineGroups)
        {
            return lineGroups
                .Select(g =>
                {
                    var cost = g.QuantitySold * g.UnitCost;
                    var profit = g.Revenue - cost;
                    return new GrossProfitRowDto
                    {
                        ProductId = g.ProductId,
                        ProductName = string.IsNullOrWhiteSpace(g.ProductName)
                            ? UnnamedProductLabel : g.ProductName.Trim(),
                        QuantitySold = g.QuantitySold,
                        Revenue = g.Revenue,
                        Cost = cost,
                        Profit = profit,
                        MarginPercent = ComputeMarginPercent(profit, g.Revenue),
                        CostPrice = g.UnitCost,
                        CostMissing = g.UnitCost <= 0m
                    };
                })
                .OrderByDescending(r => r.Profit)
                .ThenByDescending(r => r.Revenue)
                .ToList();
        }

        /// <summary>Tính dòng tổng cộng của báo cáo từ các dòng chi tiết.</summary>
        private static GrossProfitTotalDto BuildReportTotal(List<GrossProfitRowDto> rows)
        {
            var totalQty = rows.Sum(r => r.QuantitySold);
            var totalRevenue = rows.Sum(r => r.Revenue);
            var totalCost = rows.Sum(r => r.Cost);
            var totalProfit = rows.Sum(r => r.Profit);

            return new GrossProfitTotalDto
            {
                QuantitySold = totalQty,
                Revenue = totalRevenue,
                Cost = totalCost,
                Profit = totalProfit,
                MarginPercent = ComputeMarginPercent(totalProfit, totalRevenue)
            };
        }

        /// <summary>
        /// Biên lãi gộp theo phần trăm, làm tròn theo <see cref="MarginRoundDecimals"/>.
        /// Doanh thu bằng 0 thì không tính được biên lãi → trả null thay vì chia cho 0.
        /// Dùng chung cho cả dòng chi tiết và dòng tổng cộng để logic không bị lệch nhau.
        /// </summary>
        private static decimal? ComputeMarginPercent(decimal profit, decimal revenue)
        {
            return revenue > 0
                ? Math.Round(profit / revenue * PercentFactor, MarginRoundDecimals)
                : (decimal?)null;
        }

        /// <summary>
        /// Admin: toàn bộ món kèm giá vốn hiện tại, dùng cho bảng nhập giá
        /// vốn ở màn hình báo cáo lãi gộp.
        /// </summary>
        /// <returns>Danh sách món với giá vốn hiện tại.</returns>
        public virtual async Task<List<ProductCostDto>> GetProductCostsAsync()
        {
            var productQueryable = await _products.GetQueryableAsync();
            return await _asyncExecuter.ToListAsync(
                productQueryable
                    .OrderBy(x => x.Name)
                    // Giới hạn server-side: bảng sản phẩm admin quản lý thủ
                    // công, không bao giờ chạm ngưỡng nhưng tránh materialize
                    // toàn bảng (pattern như PromotionAppService).
                    .Take(500)
                    .Select(x => new ProductCostDto
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Unit = x.Unit,
                        Price = x.Price.Amount,
                        CostPrice = x.CostPrice
                    }));
        }

        /// <summary>
        /// Admin: đặt giá vốn (giá nhập) của một món. LƯU Ý: hệ thống không
        /// lưu lịch sử giá vốn nên giá mới áp dụng hồi tố — báo cáo lãi gộp
        /// các kỳ trước cũng thay đổi theo. Xóa cache catalog để trang shop
        /// và báo cáo thấy giá mới ngay.
        /// </summary>
        /// <param name="productId">Id món cần cập nhật giá vốn.</param>
        /// <param name="costPrice">Giá vốn mới (VND).</param>
        /// <returns>Thông tin món sau khi cập nhật.</returns>
        public virtual async Task<ProductCostDto> UpdateCostPriceAsync(Guid productId, decimal costPrice)
        {
            var product = await _products.GetAsync(productId);
            product.ChangeCostPrice(costPrice);
            await _products.UpdateAsync(product);

            // Bug fix (bug-hunt run 14:25): đổi giá vốn mà không invalidate
            // catalog cache (TTL 5p) → GET /api/products/{id} vẫn trả CostPrice
            // cũ, báo cáo lãi gộp sai. Mọi write path khác (ProductAppService,
            // ProductSizeOption, Topping, Combo) đã invalidate, riêng chỗ này bị sót.
            _catalogCache.Invalidate();

            return new ProductCostDto
            {
                Id = product.Id,
                Name = product.Name,
                Unit = product.Unit,
                Price = product.Price.Amount,
                CostPrice = product.CostPrice
            };
        }

        /// <summary>
        /// Sinh gợi ý kinh doanh từ số liệu: món chưa nhập giá vốn (lãi đang
        /// tính bằng 100% doanh thu), món biên lãi cao nhưng bán ít (nên đẩy
        /// bán), món biên lãi thấp (cân nhắc tăng giá/đổi nguồn nguyên liệu),
        /// món đóng góp lãi nhiều nhất.
        /// </summary>
        private static List<string> BuildSuggestions(List<GrossProfitRowDto> rows)
        {
            var suggestions = new List<string>();

            if (rows.Count == 0)
            {
                suggestions.Add("Khoảng thời gian này chưa có đơn bán nào.");
                return suggestions;
            }

            SuggestUnpricedItems(rows, suggestions);
            SuggestMarginImprovements(rows, suggestions);
            SuggestTopProfitContributor(rows, suggestions);

            return suggestions;
        }

        /// <summary>
        /// Gợi ý nhập giá vốn cho các món chưa có: lãi các món này đang tạm
        /// tính bằng 100% doanh thu nên số liệu chưa chính xác.
        /// </summary>
        private static void SuggestUnpricedItems(List<GrossProfitRowDto> rows, List<string> suggestions)
        {
            var unpriced = rows.Where(r => r.CostMissing).ToList();
            if (unpriced.Count > 0)
            {
                suggestions.Add(
                    $"Có {unpriced.Count} món chưa nhập giá vốn " +
                    $"({string.Join(", ", unpriced.Take(MaxUnpricedNamesInSuggestion).Select(r => r.ProductName))}" +
                    $"{(unpriced.Count > MaxUnpricedNamesInSuggestion ? ",…" : "")}) — lãi các món này đang tính bằng 100% doanh thu. " +
                    "Nhập giá vốn ở bảng bên dưới để số liệu chính xác.");
            }
        }

        /// <summary>
        /// Gợi ý đẩy bán món biên lãi cao nhưng bán ít, và xem lại món biên
        /// lãi thấp (tăng giá hoặc đổi nguồn nguyên liệu).
        /// </summary>
        private static void SuggestMarginImprovements(List<GrossProfitRowDto> rows, List<string> suggestions)
        {
            var priced = rows.Where(r => !r.CostMissing && r.Revenue > 0).ToList();
            if (priced.Count == 0)
            {
                return;
            }

            var medianQty = Median(priced.Select(r => (double)r.QuantitySold).ToList());
            var highMarginLowVolume = priced
                .Where(r => r.QuantitySold <= medianQty && r.MarginPercent.HasValue)
                .MaxBy(r => r.MarginPercent);
            if (highMarginLowVolume != null)
            {
                suggestions.Add(
                    $"Món \"{highMarginLowVolume.ProductName}\" có biên lãi cao nhất " +
                    $"({highMarginLowVolume.MarginPercent:0.#}%) nhưng mới bán {highMarginLowVolume.QuantitySold:N0} suất — " +
                    "nên đẩy bán (đưa lên đầu trang shop, gợi ý kèm khi khách đặt).");
            }

            var lowestMargin = priced
                .Where(r => r.MarginPercent.HasValue)
                .MinBy(r => r.MarginPercent);
            if (lowestMargin != null && lowestMargin.MarginPercent < LowMarginPercentThreshold
                && (highMarginLowVolume == null || lowestMargin.ProductId != highMarginLowVolume.ProductId))
            {
                suggestions.Add(
                    $"Món \"{lowestMargin.ProductName}\" biên lãi thấp ({lowestMargin.MarginPercent:0.#}%) — " +
                    "cân nhắc tăng giá bán hoặc tìm nguồn nguyên liệu rẻ hơn.");
            }
        }

        /// <summary>
        /// Gợi ý món đóng góp lãi gộp nhiều nhất trong kỳ.
        /// </summary>
        private static void SuggestTopProfitContributor(List<GrossProfitRowDto> rows, List<string> suggestions)
        {
            var topProfit = rows.FirstOrDefault();
            if (topProfit != null && topProfit.Profit > 0)
            {
                suggestions.Add(
                    $"Món đóng góp lãi nhiều nhất: \"{topProfit.ProductName}\" " +
                    $"({topProfit.Profit:N0}₫ lãi gộp, {topProfit.QuantitySold:N0} suất).");
            }
        }

        /// <summary>
        /// Tính trung vị (median) của danh sách số; danh sách rỗng trả 0.
        /// Dùng làm mốc "bán ít": món có số suất bán ≤ trung vị được coi là bán
        /// ít khi gợi ý đẩy bán món biên lãi cao.
        /// </summary>
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
