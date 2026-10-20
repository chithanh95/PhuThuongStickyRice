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
    /// <summary>
    /// Hợp đồng dịch vụ báo cáo đơn hàng (admin): đối soát theo kênh thanh
    /// toán trong ngày, doanh thu/lợi nhuận, báo cáo hủy đơn và món bị chê.
    /// </summary>
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
        // Số dòng tối đa file Excel và dòng bắt đầu ghi dữ liệu dùng chung:
        // xem ExcelWorksheetStyling.MaxExportRows / DataStartRowIndex.

        // Số món bán chạy nhất của báo cáo dùng chung: ReportLabels.TopProductsCount.

        /// <summary>
        /// Định dạng ngày/ngày-giờ và số tiền cho các cell trong file Excel
        /// xuất báo cáo (tập trung 1 chỗ để đổi đồng loạt khi cần).
        /// </summary>
        private const string ExcelDateFormat = "dd/MM/yyyy";
        private const string ExcelDateTimeFormat = "dd/MM/yyyy HH:mm";
        private const string ExcelMoneyFormat = "#,##0";

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
        /// tối đa <see cref="ExcelWorksheetStyling.MaxExportRows"/> dòng, mới nhất lên trước.
        /// </summary>
        /// <param name="input">Bộ lọc danh sách đơn (null = không lọc).</param>
        /// <returns>Mảng byte file .xlsx.</returns>
        public virtual async Task<byte[]> ExportOrdersAsync(GetOrdersInput input)
        {
            var query = await _orders.GetQueryableAsync();
            var filtered = await ApplyFiltersAsync(query, input ?? new GetOrdersInput());

            // Projection chung ProjectToExportRows: project OrderExportRow ngay trong
            // SQL thay vì materialize full entity Order — WriteOrderRow chỉ
            // dùng các scalar này; tổng giảm cộng 4 loại được tính ngay trong
            // projection để SQL làm 1 lần, kết quả Excel không đổi.
            var items = await _asyncExecuter.ToListAsync(
                ProjectToExportRows(
                    filtered.OrderByDescending(x => x.CreatedAt).Take(ExcelWorksheetStyling.MaxExportRows)));
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
            IXLWorksheet sheet, List<OrderExportRow> items, Dictionary<Guid, List<OrderLine>> linesByOrder)
        {
            var headers = new[]
            {
                "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT", "Địa chỉ",
                "Món", "Tổng tiền", "Phí ship", "Giảm giá",
                "Kênh thanh toán", "Trạng thái TT", "Trạng thái đơn", "Ghi chú"
            };
            WriteHeaders(sheet, headers);

            // Header vàng (#f5c518) riêng của sheet xuất đơn hàng: chữ đen
            // (nền sáng) thay vì chữ trắng như header tím các sheet khác.
            ExcelWorksheetStyling.StyleHeader(sheet.Row(1), "#f5c518", whiteFont: false);

            var rowIndex = ExcelWorksheetStyling.DataStartRowIndex;
            foreach (var order in items)
            {
                WriteOrderRow(sheet, rowIndex, order, linesByOrder);
                rowIndex++;
            }

            // perf-hunt 2026-10-20: độ rộng cố định thay AdjustToContents()
            // (ClosedXML đo font từng cell — O(dòng × cột), export 5000 dòng
            // tốn vài giây chỉ để căn cột).
            ExcelWorksheetStyling.SetFixedColumnWidths(sheet, 14, 18, 24, 14, 30, 32, 14, 12, 12, 16, 16, 16, 40);
        }

        /// <summary>
        /// Ghi một dòng đơn hàng vào sheet: text món, tổng giảm (đã cộng cả 4 loại
        /// trong projection), giờ VN, và trạng thái.
        /// </summary>
        private static void WriteOrderRow(
            IXLWorksheet sheet, int rowIndex, OrderExportRow order,
            Dictionary<Guid, List<OrderLine>> linesByOrder)
        {
            var itemsText = FormatLines(
                linesByOrder.TryGetValue(order.Id, out var ol)
                    ? ol
                    : Enumerable.Empty<OrderLine>());

            sheet.Cell(rowIndex, 1).Value = order.OrderNumber;
            // Giờ VN cho file Excel (CreatedAt lưu UTC).
            sheet.Cell(rowIndex, 2).Value = order.CreatedAt.AddHours(VietnamTime.UtcOffsetHours);
            sheet.Cell(rowIndex, 2).Style.NumberFormat.Format = ExcelDateTimeFormat;
            sheet.Cell(rowIndex, 3).Value = order.CustomerName;
            sheet.Cell(rowIndex, 4).Value = order.CustomerPhone;
            sheet.Cell(rowIndex, 5).Value = order.CustomerAddress;
            sheet.Cell(rowIndex, 6).Value = itemsText;
            sheet.Cell(rowIndex, 7).Value = order.TotalAmount;
            sheet.Cell(rowIndex, 8).Value = order.DeliveryFee;
            // Tổng tiền giảm của 1 đơn = cộng cả 4 loại: mã giảm giá + điểm
            // đổi + giảm hạng thành viên + khuyến mãi (đã cộng sẵn trong projection).
            sheet.Cell(rowIndex, 9).Value = order.DiscountTotal;
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
        /// Dòng đơn rút gọn cho export Excel: chỉ các cột cần in. Tránh
        /// materialize full entity Order (tracked, ~49 field + owned Money —
        /// change-tracker snapshot nặng) cho tối đa 5000 dòng mỗi lần xuất.
        /// Các enum giữ nguyên để map label (OrderLabels) ở client-side,
        /// vì enum.ToString() không translate được trong EF Core.
        /// </summary>
        private sealed record OrderExportRow(
            Guid Id, string OrderNumber, DateTime CreatedAt,
            string CustomerName, string CustomerPhone, string CustomerAddress,
            decimal TotalAmount, decimal DeliveryFee, decimal DiscountTotal,
            PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, OrderStatus Status,
            string Notes, string AdminNote);

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
            WriteHeaders(summary, sumHeaders);
            ExcelWorksheetStyling.StyleHeader(summary.Row(1));

            var rowIndex = ExcelWorksheetStyling.DataStartRowIndex;
            foreach (var dailyStat in daily)
            {
                summary.Cell(rowIndex, 1).Value = dailyStat.Date;
                summary.Cell(rowIndex, 1).Style.NumberFormat.Format = ExcelDateFormat;
                WriteSummaryDataRow(summary, rowIndex, dailyStat.OrderCount, dailyStat.Revenue);
                rowIndex++;
            }
            // Dòng TỔNG CỘNG: cộng dồn toàn kỳ, in đậm để tách khỏi dữ liệu ngày.
            var totalOrders = daily.Sum(x => x.OrderCount);
            var totalRevenue = daily.Sum(x => x.Revenue);
            summary.Cell(rowIndex, 1).Value = "TỔNG CỘNG";
            WriteSummaryDataRow(summary, rowIndex, totalOrders, totalRevenue);
            summary.Row(rowIndex).Style.Font.Bold = true;
            // perf-hunt 2026-10-20: độ rộng cố định thay AdjustToContents().
            ExcelWorksheetStyling.SetFixedColumnWidths(summary, 14, 10, 18, 20);
        }

        /// <summary>
        /// Ghi các cột dữ liệu (số đơn, doanh thu, giá trị TB/đơn) của 1 dòng
        /// vào sheet "Tổng hợp" — dùng chung cho từng ngày và dòng TỔNG CỘNG
        /// để không lặp lại block gán cell/format.
        /// </summary>
        /// <param name="summary">Sheet "Tổng hợp" đang dựng.</param>
        /// <param name="rowIndex">Chỉ số dòng cần ghi.</param>
        /// <param name="orderCount">Số đơn của dòng.</param>
        /// <param name="revenue">Doanh thu của dòng.</param>
        private static void WriteSummaryDataRow(IXLWorksheet summary, int rowIndex, long orderCount, decimal revenue)
        {
            summary.Cell(rowIndex, 2).Value = orderCount;
            summary.Cell(rowIndex, 3).Value = revenue;
            summary.Cell(rowIndex, 3).Style.NumberFormat.Format = ExcelMoneyFormat;
            summary.Cell(rowIndex, 4).Value = orderCount > 0 ? revenue / orderCount : 0m;
            summary.Cell(rowIndex, 4).Style.NumberFormat.Format = ExcelMoneyFormat;
        }

        /// <summary>
        /// Ghi sheet "Chi tiết đơn" của file Excel doanh thu: từng đơn trong kỳ
        /// (mã đơn, ngày đặt giờ VN, khách, món, tổng tiền, kênh TT, trạng thái).
        /// </summary>
        /// <param name="workbook">Workbook Excel đang dựng.</param>
        /// <param name="details">Danh sách đơn trong kỳ, mới nhất lên trước.</param>
        /// <param name="linesByOrder">Dòng món theo Id đơn (load 1 query gộp).</param>
        private static void WriteRevenueDetailSheet(
            XLWorkbook workbook, List<OrderExportRow> details, Dictionary<Guid, List<OrderLine>> linesByOrder)
        {
            var sheet = workbook.Worksheets.Add("Chi tiết đơn");
            var headers = new[]
            {
                "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT",
                "Món", "Tổng tiền", "Kênh thanh toán", "Trạng thái đơn"
            };
            WriteHeaders(sheet, headers);
            ExcelWorksheetStyling.StyleHeader(sheet.Row(1));

            var rowIndex = ExcelWorksheetStyling.DataStartRowIndex;
            foreach (var order in details)
            {
                var itemsText = FormatLines(
                    linesByOrder.TryGetValue(order.Id, out var orderLines)
                        ? orderLines
                        : Enumerable.Empty<OrderLine>());
                sheet.Cell(rowIndex, 1).Value = order.OrderNumber;
                // Giờ VN cho file Excel (CreatedAt lưu UTC).
                sheet.Cell(rowIndex, 2).Value = order.CreatedAt.AddHours(VietnamTime.UtcOffsetHours);
                sheet.Cell(rowIndex, 2).Style.NumberFormat.Format = ExcelDateTimeFormat;
                sheet.Cell(rowIndex, 3).Value = order.CustomerName;
                sheet.Cell(rowIndex, 4).Value = order.CustomerPhone;
                sheet.Cell(rowIndex, 5).Value = itemsText;
                sheet.Cell(rowIndex, 6).Value = order.TotalAmount;
                sheet.Cell(rowIndex, 6).Style.NumberFormat.Format = ExcelMoneyFormat;
                sheet.Cell(rowIndex, 7).Value = OrderLabels.PaymentMethod(order.PaymentMethod);
                sheet.Cell(rowIndex, 8).Value = OrderLabels.OrderStatus(order.Status);
                rowIndex++;
            }
            // perf-hunt 2026-10-20: độ rộng cố định thay AdjustToContents().
            ExcelWorksheetStyling.SetFixedColumnWidths(sheet, 14, 18, 24, 14, 40, 14, 16, 16);
        }

        /// <summary>
        /// Xuất Excel doanh thu kỳ Việt Nam: sheet "Tổng hợp" (1 dòng/ngày: số đơn,
        /// doanh thu, giá trị TB/đơn + dòng TỔNG CỘNG) và sheet "Chi tiết đơn"
        /// (mới nhất lên trước, tối đa <see cref="ExcelWorksheetStyling.MaxExportRows"/> dòng).
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

            // perf-hunt 2026-10-19 (run 17:55): projection chung
            // ProjectToExportRows (không materialize full entity Order tracked).
            var details = await _asyncExecuter.ToListAsync(
                ProjectToExportRows(
                    ranged.OrderByDescending(x => x.CreatedAt).Take(ExcelWorksheetStyling.MaxExportRows)));
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

            // Gộp count + sum thành 1 query (GROUP BY hằng số): trước đây là
            // 2 round-trip riêng trên cùng tập billable.
            var (orderCount, totalRevenue) = await LoadRevenueTotalsAsync(billable);

            var topProducts = await LoadTopProductsAsync(billable);

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
        /// Đếm đơn + tổng doanh thu của tập đơn thanh toán được (đã trừ đơn
        /// hủy) bằng 1 query SQL (GROUP BY hằng số) — không materialize toàn
        /// bộ đơn + dòng đơn về memory. Sum trên tập rỗng trả NULL trong SQL
        /// nên doanh thu phải ?? 0m để giữ đúng hành vi cũ.
        /// </summary>
        /// <param name="billable">Query đơn đã lọc khoảng ngày, trừ đơn hủy.</param>
        /// <returns>(Số đơn, tổng doanh thu VND).</returns>
        private async Task<(long OrderCount, decimal TotalRevenue)> LoadRevenueTotalsAsync(
            IQueryable<Order> billable)
        {
            var totals = await _asyncExecuter.FirstOrDefaultAsync(
                billable.GroupBy(x => 1)
                    .Select(g => new
                    {
                        OrderCount = g.LongCount(),
                        TotalRevenue = g.Sum(x => (decimal?)x.TotalAmount.Amount)
                    }));
            return (totals?.OrderCount ?? 0L, totals?.TotalRevenue ?? 0m);
        }

        /// <summary>
        /// Top <see cref="TopProductsCount"/> món bán chạy nhất (theo số lượng,
        /// giảm dần) của tập đơn thanh toán được: gom số lượng + doanh thu theo
        /// tên món ngay trong SQL, không tải dòng đơn về memory.
        /// </summary>
        /// <param name="billable">Query đơn đã lọc khoảng ngày, trừ đơn hủy.</param>
        /// <returns>Danh sách top món (xem <see cref="ReportLabels.TopProductsCount"/>), doanh thu tính theo đơn giá thực tế.</returns>
        private async Task<List<RevenueSummaryTopProductDto>> LoadTopProductsAsync(
            IQueryable<Order> billable)
        {
            return await _asyncExecuter.ToListAsync(
                billable.SelectMany(x => x.Lines)
                    .GroupBy(l => l.ProductName)
                    .Select(g => new RevenueSummaryTopProductDto
                    {
                        ProductName = g.Key,
                        Quantity = g.Sum(l => (long)l.Quantity),
                        Revenue = g.Sum(l => l.LineTotal.Amount)
                    })
                    .OrderByDescending(x => x.Quantity)
                    .Take(ReportLabels.TopProductsCount));
        }

        /// <summary>
        /// Ghi dòng tiêu đề (dòng 1) của 1 sheet xuất Excel từ mảng tên cột —
        /// gom loop ghi cell lặp y hệt ở 3 sheet writer (WriteOrdersSheet,
        /// WriteRevenueSummarySheet, WriteRevenueDetailSheet).
        /// </summary>
        private static void WriteHeaders(IXLWorksheet sheet, string[] headers)
        {
            for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            {
                sheet.Cell(1, columnIndex + 1).Value = headers[columnIndex];
            }
        }

        /// <summary>
        /// Projection chung Order → OrderExportRow cho 2 export (đơn hàng và chi
        /// tiết doanh thu): gom khối Select 13 field từng copy-paste y hệt ở 2 nơi.
        /// Giữ projection trong SQL (không materialize full entity Order tracked) —
        /// tổng giảm cộng cả 4 loại giảm lưu riêng được tính ngay trong projection.
        /// </summary>
        /// <param name="query">Query Order đã lọc/sắp xếp/giới hạn dòng.</param>
        /// <returns>Query project sang <see cref="OrderExportRow"/>.</returns>
        private static IQueryable<OrderExportRow> ProjectToExportRows(IQueryable<Order> query)
        {
            return query.Select(x => new OrderExportRow(
                x.Id, x.OrderNumber, x.CreatedAt,
                x.CustomerName, x.CustomerPhone, x.CustomerAddress,
                x.TotalAmount.Amount, x.DeliveryFee.Amount,
                x.DiscountAmount.Amount + x.LoyaltyDiscount.Amount
                    + x.TierDiscount.Amount + x.PromotionDiscountAmount,
                x.PaymentMethod, x.PaymentStatus, x.Status,
                x.Notes, x.AdminNote));
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
        /// Lọc đơn theo filter danh sách: tìm kiếm / trạng thái / shipper /
        /// khoảng ngày / thu tiền / chỉ VIP — phải đồng nhất với GetListAsync
        /// ở OrderAppService để file Excel xuất ra khớp danh sách đang xem.
        /// </summary>
        private async Task<IQueryable<Order>> ApplyFiltersAsync(IQueryable<Order> query, GetOrdersInput input)
        {
            // CashCollected đã nằm trong OrderListFilters.Apply (SH5/bug-hunt:
            // export từng bỏ sót filter này khiến file Excel lệch so với danh sách).
            query = OrderListFilters.Apply(query, input);

            // Item 130 (đúng): only orders whose customer is marked VIP.
            if (input.VipOnly)
            {
                var vipPhones = await _vipPriority.GetVipPhonesAsync();
                query = query.Where(x => vipPhones.Contains(x.CustomerPhone));
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
