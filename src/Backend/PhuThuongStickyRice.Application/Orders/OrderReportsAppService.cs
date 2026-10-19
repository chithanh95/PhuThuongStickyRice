using ClosedXML.Excel;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Customer;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IOrderReportsAppService : IApplicationService
    {
        /// <summary>
        /// Wave 6c (item 31): per-payment-channel reconciliation of one
        /// Vietnam day. Null <paramref name="date"/> = today.
        /// </summary>
        Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime? date);

        /// <summary>
        /// Wave 6c (item 32): exports the orders matching the given filters
        /// (search / status / shipper / date range) to an .xlsx file.
        /// Capped at 5000 rows.
        /// </summary>
        Task<byte[]> ExportOrdersAsync(GetOrdersInput input);

        /// <summary>
        /// Revenue report: 2 sheets — daily summary (date, order count,
        /// revenue, average order value) and order details — for the given
        /// Vietnam date range. Capped at 5000 detail rows.
        /// </summary>
        Task<byte[]> ExportRevenueAsync(DateTime? from, DateTime? to);

        /// <summary>
        /// Item 169: revenue summary of one Vietnam date range — order
        /// count, total revenue, average order value and top 5 products.
        /// Cancelled orders are excluded. Used by the compare view (the
        /// frontend calls it once per period).
        /// </summary>
        Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateTime? from, DateTime? to);
    }

    /// <summary>
    /// Báo cáo doanh thu cho admin: đối soát 1 ngày VN theo kênh thanh toán,
    /// xuất Excel đơn hàng / doanh thu (tối đa 5000 dòng để file không phình),
    /// tổng hợp doanh thu theo khoảng ngày. Toàn bộ chỉ role Admin được gọi.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class OrderReportsAppService : ApplicationService, IOrderReportsAppService
    {
        private const int MaxExportRows = 5000;

        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IVipPriorityAppService _vipPriority;

        public OrderReportsAppService(
            IOrderRepository orders,
            IAsyncQueryableExecuter asyncExecuter,
            IVipPriorityAppService vipPriority)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
            _vipPriority = vipPriority;
        }

        /// <summary>
        /// Một dòng đối soát theo kênh thanh toán (record lồng để truyền
        /// dữ liệu query qua <see cref="BuildDailyReconciliationDto"/> thay
        /// cho anonymous type).
        /// </summary>
        private sealed record ReconciliationChannelRow(
            PaymentMethod Method, long OrderCount, decimal Revenue, decimal PaidAmount, decimal UnpaidAmount);

        /// <summary>
        /// Đối soát doanh thu 1 ngày Việt Nam theo kênh thanh toán: số đơn, doanh thu,
        /// đã/chưa thanh toán từng kênh. Đơn đã hủy chỉ tính vào số đơn, không tính
        /// vào doanh thu. <paramref name="date"/> null = hôm nay.
        /// </summary>
        public virtual async Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime? date)
        {
            var day = (date ?? VietnamTime.Today()).Date;
            var startUtc = VietnamTime.DayStartUtc(day);
            var endUtc = VietnamTime.DayEndExclusiveUtc(day);

            var queryable = await _orders.GetQueryableAsync();
            var rows = await LoadReconciliationRowsAsync(
                queryable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc));

            return BuildDailyReconciliationDto(day, rows);
        }

        /// <summary>
        /// Gộp 1 query theo kênh thanh toán: đếm đơn, doanh thu, đã/chưa thanh
        /// toán. Đơn hủy chỉ tính vào số đơn, không tính vào doanh thu.
        /// </summary>
        /// <param name="dayQuery">Query đơn trong ngày VN (giờ UTC).</param>
        private async Task<List<ReconciliationChannelRow>> LoadReconciliationRowsAsync(
            IQueryable<Order> dayQuery)
        {
            return await _asyncExecuter.ToListAsync(
                dayQuery
                    .GroupBy(x => x.PaymentMethod)
                    .Select(g => new ReconciliationChannelRow(
                        g.Key,
                        g.LongCount(),
                        g.Where(x => x.Status != OrderStatus.Cancelled)
                            .Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m,
                        g.Where(x => x.Status != OrderStatus.Cancelled
                                && x.PaymentStatus == PaymentStatus.Paid)
                            .Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m,
                        g.Where(x => x.Status != OrderStatus.Cancelled
                                && x.PaymentStatus != PaymentStatus.Paid)
                            .Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m)));
        }

        /// <summary>
        /// Dựng DTO đối soát: map từng kênh thanh toán (kênh nào không có đơn
        /// trong ngày thì toàn bộ chỉ số = 0) rồi cộng dồn hàng tổng.
        /// </summary>
        private static DailyReconciliationDto BuildDailyReconciliationDto(
            DateTime day, List<ReconciliationChannelRow> rows)
        {
            var channels = Enum.GetValues<PaymentMethod>()
                .Select(m =>
                {
                    var row = rows.FirstOrDefault(x => x.Method == m);
                    return new ReconciliationChannelDto
                    {
                        PaymentMethod = m.ToString(),
                        Label = OrderLabels.PaymentMethod(m),
                        OrderCount = row?.OrderCount ?? 0,
                        Revenue = row?.Revenue ?? 0m,
                        PaidAmount = row?.PaidAmount ?? 0m,
                        UnpaidAmount = row?.UnpaidAmount ?? 0m
                    };
                })
                .ToList();

            return new DailyReconciliationDto
            {
                Date = day,
                Channels = channels,
                TotalOrderCount = channels.Sum(x => x.OrderCount),
                TotalRevenue = channels.Sum(x => x.Revenue),
                TotalPaidAmount = channels.Sum(x => x.PaidAmount),
                TotalUnpaidAmount = channels.Sum(x => x.UnpaidAmount)
            };
        }

        /// <summary>
        /// Xuất Excel danh sách đơn hàng theo bộ lọc đang xem (tìm kiếm / trạng thái /
        /// shipper / khoảng ngày / thu tiền / chỉ VIP — đồng bộ với danh sách ở
        /// OrderAppService.GetListAsync để file khớp màn hình). 1 sheet "Đơn hàng",
        /// tối đa <see cref="MaxExportRows"/> dòng, mới nhất lên trước.
        /// </summary>
        /// <param name="input">Bộ lọc danh sách đơn (null = không lọc).</param>
        /// <returns>Mảng byte file .xlsx.</returns>
        public virtual async Task<byte[]> ExportOrdersAsync(GetOrdersInput input)
        {
            var query = await _orders.GetQueryableAsync();
            var filtered = await ApplyFiltersAsync(query, input ?? new GetOrdersInput());

            var items = await _asyncExecuter.ToListAsync(
                filtered
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(MaxExportRows));
            var linesByOrder = await GetLinesByOrderAsync(items.Select(x => x.Id));

            using var workbook = new XLWorkbook();
            WriteOrdersSheet(workbook.Worksheets.Add("Đơn hàng"), items, linesByOrder);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Ghi sheet "Đơn hàng" của file Excel xuất đơn: header vàng + 1 dòng/đơn
        /// (ngày đặt quy về giờ VN, tổng giảm = cộng cả 4 loại giảm lưu riêng trong DB).
        /// </summary>
        /// <param name="sheet">Sheet Excel đang dựng.</param>
        /// <param name="items">Đơn hàng cần xuất, mới nhất lên trước.</param>
        /// <param name="linesByOrder">Dòng món theo Id đơn (load 1 query gộp).</param>
        private static void WriteOrdersSheet(
            IXLWorksheet sheet, List<Order> items, Dictionary<Guid, List<OrderLine>> linesByOrder)
        {
            var headers = new[]
            {
                "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT", "Địa chỉ",
                "Món", "Tổng tiền", "Phí ship", "Giảm giá",
                "Kênh thanh toán", "Trạng thái TT", "Trạng thái đơn", "Ghi chú"
            };
            for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                sheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
            }

            var headerRow = sheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#f5c518");
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var rowIndex = 2;
            foreach (var order in items)
            {
                WriteOrderRow(sheet, rowIndex, order, linesByOrder);
                rowIndex++;
            }

            sheet.Columns().AdjustToContents();
        }

        /// <summary>
        /// Ghi một dòng đơn hàng vào sheet: text món, tổng giảm cộng cả 4 loại
        /// (mã giảm giá + điểm đổi + giảm hạng thành viên + khuyến mãi —
        /// trong DB chúng lưu riêng), giờ VN, và trạng thái.
        /// </summary>
        private static void WriteOrderRow(
            IXLWorksheet sheet, int rowIndex, Order order,
            Dictionary<Guid, List<OrderLine>> linesByOrder)
        {
            var itemsText = FormatLines(
                linesByOrder.TryGetValue(order.Id, out var ol)
                    ? ol
                    : Enumerable.Empty<OrderLine>());
            // Tổng tiền giảm của 1 đơn = cộng cả 4 loại: mã giảm giá + điểm
            // đổi + giảm hạng thành viên + khuyến mãi (trong DB chúng lưu riêng).
            var discountTotal = order.DiscountAmount.Amount
                + order.LoyaltyDiscount.Amount
                + order.TierDiscount.Amount
                + order.PromotionDiscountAmount;

            sheet.Cell(rowIndex, 1).Value = order.OrderNumber;
            // Giờ VN cho file Excel (CreatedAt lưu UTC).
            sheet.Cell(rowIndex, 2).Value = order.CreatedAt.AddHours(VietnamTime.UtcOffsetHours);
            sheet.Cell(rowIndex, 2).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
            sheet.Cell(rowIndex, 3).Value = order.CustomerName;
            sheet.Cell(rowIndex, 4).Value = order.CustomerPhone;
            sheet.Cell(rowIndex, 5).Value = order.CustomerAddress;
            sheet.Cell(rowIndex, 6).Value = itemsText;
            sheet.Cell(rowIndex, 7).Value = order.TotalAmount.Amount;
            sheet.Cell(rowIndex, 8).Value = order.DeliveryFee.Amount;
            sheet.Cell(rowIndex, 9).Value = discountTotal;
            sheet.Cell(rowIndex, 10).Value = OrderLabels.PaymentMethod(order.PaymentMethod);
            sheet.Cell(rowIndex, 11).Value = OrderLabels.PaymentStatus(order.PaymentStatus);
            sheet.Cell(rowIndex, 12).Value = OrderLabels.OrderStatus(order.Status);
            sheet.Cell(rowIndex, 13).Value = string.Join(" | ",
                new[] { order.Notes, order.AdminNote }
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Replace("\r\n", " ").Replace("\n", " ")));
        }

        /// <summary>
        /// Một dòng tổng hợp doanh thu theo ngày (giờ VN) — record lồng để
        /// truyền dữ liệu query qua <see cref="WriteRevenueSummarySheet"/>
        /// thay cho anonymous type.
        /// </summary>
        private sealed record DailyRevenueRow(DateTime Date, long OrderCount, decimal Revenue);

        /// <summary>
        /// Ghi sheet "Tổng hợp" của file Excel doanh thu: 1 dòng/ngày (số đơn,
        /// doanh thu, giá trị TB/đơn) + dòng TỔNG CỘNG in đậm cuối bảng.
        /// </summary>
        /// <param name="workbook">Workbook Excel đang dựng.</param>
        /// <param name="daily">Doanh thu từng ngày (giờ VN), đã sắp xếp tăng dần.</param>
        private static void WriteRevenueSummarySheet(XLWorkbook workbook, List<DailyRevenueRow> daily)
        {
            var summary = workbook.Worksheets.Add("Tổng hợp");
            var sumHeaders = new[] { "Ngày", "Số đơn", "Doanh thu (₫)", "Giá trị TB/đơn (₫)" };
            for (var columnIndex = 0; columnIndex < sumHeaders.Length; columnIndex++)
                summary.Cell(1, columnIndex + 1).Value = sumHeaders[columnIndex];
            StyleHeader(summary.Row(1));

            var rowIndex = 2;
            foreach (var dailyStat in daily)
            {
                summary.Cell(rowIndex, 1).Value = dailyStat.Date;
                summary.Cell(rowIndex, 1).Style.NumberFormat.Format = "dd/MM/yyyy";
                summary.Cell(rowIndex, 2).Value = dailyStat.OrderCount;
                summary.Cell(rowIndex, 3).Value = dailyStat.Revenue;
                summary.Cell(rowIndex, 3).Style.NumberFormat.Format = "#,##0";
                summary.Cell(rowIndex, 4).Value = dailyStat.OrderCount > 0 ? dailyStat.Revenue / dailyStat.OrderCount : 0m;
                summary.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
                rowIndex++;
            }
            // Dòng TỔNG CỘNG: cộng dồn toàn kỳ, in đậm để tách khỏi dữ liệu ngày.
            var totalOrders = daily.Sum(x => x.OrderCount);
            var totalRevenue = daily.Sum(x => x.Revenue);
            summary.Cell(rowIndex, 1).Value = "TỔNG CỘNG";
            summary.Cell(rowIndex, 2).Value = totalOrders;
            summary.Cell(rowIndex, 3).Value = totalRevenue;
            summary.Cell(rowIndex, 3).Style.NumberFormat.Format = "#,##0";
            summary.Cell(rowIndex, 4).Value = totalOrders > 0 ? totalRevenue / totalOrders : 0m;
            summary.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
            summary.Row(rowIndex).Style.Font.Bold = true;
            summary.Columns().AdjustToContents();
        }

        /// <summary>
        /// Ghi sheet "Chi tiết đơn" của file Excel doanh thu: từng đơn trong kỳ
        /// (mã đơn, ngày đặt giờ VN, khách, món, tổng tiền, kênh TT, trạng thái).
        /// </summary>
        /// <param name="workbook">Workbook Excel đang dựng.</param>
        /// <param name="details">Danh sách đơn trong kỳ, mới nhất lên trước.</param>
        /// <param name="linesByOrder">Dòng món theo Id đơn (load 1 query gộp).</param>
        private static void WriteRevenueDetailSheet(
            XLWorkbook workbook, List<Order> details, Dictionary<Guid, List<OrderLine>> linesByOrder)
        {
            var sheet = workbook.Worksheets.Add("Chi tiết đơn");
            var headers = new[]
            {
                "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT",
                "Món", "Tổng tiền", "Kênh thanh toán", "Trạng thái đơn"
            };
            for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
                sheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
            StyleHeader(sheet.Row(1));

            var rowIndex = 2;
            foreach (var order in details)
            {
                var itemsText = FormatLines(
                    linesByOrder.TryGetValue(order.Id, out var orderLines)
                        ? orderLines
                        : Enumerable.Empty<OrderLine>());
                sheet.Cell(rowIndex, 1).Value = order.OrderNumber;
                // Giờ VN cho file Excel (CreatedAt lưu UTC).
                sheet.Cell(rowIndex, 2).Value = order.CreatedAt.AddHours(VietnamTime.UtcOffsetHours);
                sheet.Cell(rowIndex, 2).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
                sheet.Cell(rowIndex, 3).Value = order.CustomerName;
                sheet.Cell(rowIndex, 4).Value = order.CustomerPhone;
                sheet.Cell(rowIndex, 5).Value = itemsText;
                sheet.Cell(rowIndex, 6).Value = order.TotalAmount.Amount;
                sheet.Cell(rowIndex, 6).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(rowIndex, 7).Value = OrderLabels.PaymentMethod(order.PaymentMethod);
                sheet.Cell(rowIndex, 8).Value = OrderLabels.OrderStatus(order.Status);
                rowIndex++;
            }
            sheet.Columns().AdjustToContents();
        }

        /// <summary>
        /// Xuất Excel doanh thu kỳ Việt Nam: sheet "Tổng hợp" (1 dòng/ngày: số đơn,
        /// doanh thu, giá trị TB/đơn + dòng TỔNG CỘNG) và sheet "Chi tiết đơn"
        /// (mới nhất lên trước, tối đa <see cref="MaxExportRows"/> dòng).
        /// Đơn đã hủy không tính doanh thu nhưng vẫn xuất trong chi tiết.
        /// </summary>
        /// <param name="from">Ngày bắt đầu kỳ (null = từ đầu).</param>
        /// <param name="to">Ngày kết thúc kỳ (null = tới hiện tại).</param>
        /// <returns>Mảng byte file .xlsx.</returns>
        public virtual async Task<byte[]> ExportRevenueAsync(DateTime? from, DateTime? to)
        {
            var queryable = await _orders.GetQueryableAsync();
            var startUtc = from.HasValue ? VietnamTime.DayStartUtc(from.Value) : DateTime.MinValue;
            var endUtc = to.HasValue ? VietnamTime.DayEndExclusiveUtc(to.Value) : DateTime.MaxValue;
            var ranged = queryable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc);
            var billable = ranged.Where(x => x.Status != OrderStatus.Cancelled);

            // Tổng hợp theo ngày VN (không tính đơn đã hủy) — dùng record lồng
            // để truyền qua helper ghi sheet, thay cho anonymous type.
            var daily = await _asyncExecuter.ToListAsync(
                billable.GroupBy(x => x.CreatedAt.AddHours(VietnamTime.UtcOffsetHours).Date)
                    .Select(g => new DailyRevenueRow(
                        g.Key,
                        g.LongCount(),
                        g.Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m))
                    .OrderBy(x => x.Date));

            var details = await _asyncExecuter.ToListAsync(
                ranged.OrderByDescending(x => x.CreatedAt).Take(MaxExportRows));
            var linesByOrder = await GetLinesByOrderAsync(details.Select(x => x.Id));

            using var workbook = new XLWorkbook();
            WriteRevenueSummarySheet(workbook, daily);
            WriteRevenueDetailSheet(workbook, details, linesByOrder);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Item 169: revenue summary of one Vietnam date range (cancelled
        /// orders excluded). The compare view calls this once per period.
        /// </summary>
        public virtual async Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateTime? from, DateTime? to)
        {
            var queryable = await _orders.GetQueryableAsync();
            var startUtc = from.HasValue ? VietnamTime.DayStartUtc(from.Value) : DateTime.MinValue;
            var endUtc = to.HasValue ? VietnamTime.DayEndExclusiveUtc(to.Value) : DateTime.MaxValue;

            // Aggregates computed in SQL — never materialize all orders+lines.
            var billable = queryable
                .Where(x => x.CreatedAt >= startUtc
                    && x.CreatedAt < endUtc
                    && x.Status != OrderStatus.Cancelled);

            var orderCount = await _asyncExecuter.LongCountAsync(billable);
            var totalRevenue = await _asyncExecuter.SumAsync(billable.Select(x => x.TotalAmount.Amount));

            var topProducts = await _asyncExecuter.ToListAsync(
                billable.SelectMany(x => x.Lines)
                    .GroupBy(l => l.ProductName)
                    .Select(g => new RevenueSummaryTopProductDto
                    {
                        ProductName = g.Key,
                        Quantity = g.Sum(l => (long)l.Quantity),
                        Revenue = g.Sum(l => l.LineTotal.Amount)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(5));

            return new RevenueSummaryDto
            {
                From = from?.Date,
                To = to?.Date,
                OrderCount = orderCount,
                TotalRevenue = totalRevenue,
                AvgOrderValue = orderCount > 0 ? totalRevenue / orderCount : 0m,
                TopProducts = topProducts
            };
        }

        /// <summary>
        /// Style hàng tiêu đề file Excel: chữ đậm trắng, nền tím thương hiệu
        /// (#7a6fbe), căn giữa — dùng chung cho mọi sheet xuất ra.
        /// </summary>
        private static void StyleHeader(IXLRow headerRow)
        {
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#7a6fbe");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        /// <summary>
        /// Loads order lines for the given orders in ONE extra query, keyed
        /// by order id. Needed because AutoInclude for Order.Lines was removed
        /// (see OrderConfiguration) and this layer cannot call EF Core's
        /// Include directly — so the exports would otherwise always show an
        /// empty "Món" column.
        /// </summary>
        private async Task<Dictionary<Guid, List<OrderLine>>> GetLinesByOrderAsync(
            IEnumerable<Guid> orderIds)
        {
            var ids = orderIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, List<OrderLine>>();
            }

            var ordersWithLines = await _orders.GetListWithLinesAsync(ids);
            return ordersWithLines
                .ToDictionary(x => x.Id, x => x.Lines.ToList());
        }

        /// <summary>
        /// Renders order lines as "2× Xôi gấc (Lớn); 1× Xôi xéo" for exports.
        /// </summary>
        private static string FormatLines(IEnumerable<OrderLine> lines) =>
            string.Join("; ",
                lines.Select(x =>
                    $"{x.Quantity}× {x.ProductName}" +
                    (string.IsNullOrWhiteSpace(x.SizeName) ? "" : $" ({x.SizeName})")));
        /// <summary>
        /// Lọc đơn xuất Excel theo ô tìm kiếm (mã đơn/tên khách/SĐT), trạng
        /// thái, shipper và khoảng ngày VN. Trạng thái nhập sai chính tả thì
        /// bỏ qua bộ lọc (không lỗi trang).
        /// </summary>
        /// <summary>
        /// Lọc đơn theo filter danh sách: tìm kiếm / trạng thái / shipper /
        /// khoảng ngày / thu tiền / chỉ VIP — phải đồng nhất với GetListAsync
        /// ở OrderAppService để file Excel xuất ra khớp danh sách đang xem.
        /// </summary>
        private async Task<IQueryable<Order>> ApplyFiltersAsync(IQueryable<Order> query, GetOrdersInput input)
        {
            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x =>
                    x.OrderNumber.Contains(term)
                    || x.CustomerName.Contains(term)
                    || x.CustomerPhone.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<OrderStatus>(input.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (input.ShipperId.HasValue && input.ShipperId.Value != Guid.Empty)
            {
                var shipperId = input.ShipperId.Value;
                query = query.Where(x => x.ShipperId == shipperId);
            }

            if (input.FromDate.HasValue || input.ToDate.HasValue)
            {
                query = ApplyDateRangeFilter(query, input);
            }

            // SH5 (bug-hunt): export bỏ sót 2 filter này nên file Excel lệch
            // âm thầm so với danh sách — bổ sung cho khớp GetListAsync.
            // Item 163: filter by cash-collection state.
            if (input.CashCollected.HasValue)
            {
                var collected = input.CashCollected.Value;
                query = query.Where(x => x.CashCollected == collected);
            }

            // Item 130 (đúng): only orders whose customer is marked VIP.
            if (input.VipOnly)
            {
                var vipPhones = await _vipPriority.GetVipPhonesAsync();
                query = query.Where(x => vipPhones.Contains(x.CustomerPhone));
            }

            return query;
        }

        /// <summary>
        /// Lọc đơn theo khoảng ngày Việt Nam: đầu ngày FromDate (00:00, biên
        /// bao) đến hết ngày ToDate (biên độc quyền) — cùng cách đổi múi giờ
        /// với GetListAsync ở OrderAppService để Excel khớp danh sách đang xem.
        /// </summary>
        private static IQueryable<Order> ApplyDateRangeFilter(IQueryable<Order> query, GetOrdersInput input)
        {
            if (input.FromDate.HasValue)
            {
                var fromUtc = VietnamTime.DayStartUtc(input.FromDate.Value);
                query = query.Where(x => x.CreatedAt >= fromUtc);
            }

            if (input.ToDate.HasValue)
            {
                var toUtc = VietnamTime.DayEndExclusiveUtc(input.ToDate.Value);
                query = query.Where(x => x.CreatedAt < toUtc);
            }

            return query;
        }
    }

    /// <summary>
    /// Nhãn tiếng Việt cho enum đơn hàng dùng trong file Excel/báo cáo —
    /// internal vì chỉ phục vụ xuất báo cáo, không expose qua API.
    /// </summary>
    internal static class OrderLabels
    {
        /// <summary>Nhãn tiếng Việt của kênh thanh toán (COD / Chuyển khoản / MoMo / Ví).</summary>
        public static string PaymentMethod(PaymentMethod method) => method switch
        {
            Domain.Entities.PaymentMethod.Cod => "COD",
            Domain.Entities.PaymentMethod.BankTransfer => "Chuyển khoản",
            Domain.Entities.PaymentMethod.MoMo => "MoMo",
            Domain.Entities.PaymentMethod.Wallet => "Ví",
            _ => method.ToString()
        };

        /// <summary>Nhãn tiếng Việt của trạng thái thanh toán (Chưa thanh toán / Chờ xác nhận / Đã thanh toán / Thất bại).</summary>
        public static string PaymentStatus(PaymentStatus status) => status switch
        {
            Domain.Entities.PaymentStatus.Unpaid => "Chưa thanh toán",
            Domain.Entities.PaymentStatus.PendingConfirmation => "Chờ xác nhận",
            Domain.Entities.PaymentStatus.Paid => "Đã thanh toán",
            Domain.Entities.PaymentStatus.Failed => "Thất bại",
            _ => status.ToString()
        };

        /// <summary>Nhãn tiếng Việt của trạng thái đơn (Chờ xác nhận / Đã xác nhận / Đang chuẩn bị / Đang giao / Hoàn thành / Đã hủy).</summary>
        public static string OrderStatus(OrderStatus status) => status switch
        {
            Domain.Entities.OrderStatus.Pending => "Chờ xác nhận",
            Domain.Entities.OrderStatus.Confirmed => "Đã xác nhận",
            Domain.Entities.OrderStatus.Preparing => "Đang chuẩn bị",
            Domain.Entities.OrderStatus.Delivering => "Đang giao",
            Domain.Entities.OrderStatus.Completed => "Hoàn thành",
            Domain.Entities.OrderStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }
}
