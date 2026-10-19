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

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class OrderReportsAppService : ApplicationService, IOrderReportsAppService
    {
        private const int MaxExportRows = 5000;

        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public OrderReportsAppService(
            IOrderRepository orders,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _orders = orders;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<DailyReconciliationDto> GetDailyReconciliationAsync(DateTime? date)
        {
            var day = (date ?? VietnamTime.Today()).Date;
            var startUtc = VietnamTime.DayStartUtc(day);
            var endUtc = VietnamTime.DayEndExclusiveUtc(day);

            var queryable = await _orders.GetQueryableAsync();
            var dayQuery = queryable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc);

            // One query per channel metric is avoided: group once by payment
            // method and derive count / revenue / paid / unpaid in the
            // projection. Cancelled orders contribute to the count but never
            // to revenue.
            var rows = await _asyncExecuter.ToListAsync(
                dayQuery
                    .GroupBy(x => x.PaymentMethod)
                    .Select(g => new
                    {
                        Method = g.Key,
                        OrderCount = g.LongCount(),
                        Revenue = g.Where(x => x.Status != OrderStatus.Cancelled)
                            .Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m,
                        PaidAmount = g.Where(x => x.Status != OrderStatus.Cancelled
                                && x.PaymentStatus == PaymentStatus.Paid)
                            .Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m,
                        UnpaidAmount = g.Where(x => x.Status != OrderStatus.Cancelled
                                && x.PaymentStatus != PaymentStatus.Paid)
                            .Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m
                    }));

            var channels = Enum.GetValues(typeof(PaymentMethod))
                .Cast<PaymentMethod>()
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

        public virtual async Task<byte[]> ExportOrdersAsync(GetOrdersInput input)
        {
            var query = await _orders.GetQueryableAsync();
            var filtered = ApplyFilters(query, input ?? new GetOrdersInput());

            var items = await _asyncExecuter.ToListAsync(
                filtered
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(MaxExportRows));

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Đơn hàng");

            var headers = new[]
            {
                "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT", "Địa chỉ",
                "Món", "Tổng tiền", "Phí ship", "Giảm giá",
                "Kênh thanh toán", "Trạng thái TT", "Trạng thái đơn", "Ghi chú"
            };
            for (var c = 0; c < headers.Length; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
            }

            var headerRow = sheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#f5c518");
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var row = 2;
            foreach (var order in items)
            {
                var itemsText = string.Join("; ",
                    order.Lines.Select(x =>
                        $"{x.Quantity}× {x.ProductName}" +
                        (string.IsNullOrWhiteSpace(x.SizeName) ? "" : $" ({x.SizeName})")));
                var discountTotal = order.DiscountAmount.Amount
                    + order.LoyaltyDiscount.Amount
                    + order.TierDiscount.Amount
                    + order.PromotionDiscountAmount;

                sheet.Cell(row, 1).Value = order.OrderNumber;
                sheet.Cell(row, 2).Value = order.CreatedAt.AddHours(7);
                sheet.Cell(row, 2).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
                sheet.Cell(row, 3).Value = order.CustomerName;
                sheet.Cell(row, 4).Value = order.CustomerPhone;
                sheet.Cell(row, 5).Value = order.CustomerAddress;
                sheet.Cell(row, 6).Value = itemsText;
                sheet.Cell(row, 7).Value = order.TotalAmount.Amount;
                sheet.Cell(row, 8).Value = order.DeliveryFee.Amount;
                sheet.Cell(row, 9).Value = discountTotal;
                sheet.Cell(row, 10).Value = OrderLabels.PaymentMethod(order.PaymentMethod);
                sheet.Cell(row, 11).Value = OrderLabels.PaymentStatus(order.PaymentStatus);
                sheet.Cell(row, 12).Value = OrderLabels.OrderStatus(order.Status);
                sheet.Cell(row, 13).Value = string.Join(" | ",
                    new[] { order.Notes, order.AdminNote }
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Replace("\r\n", " ").Replace("\n", " ")));
                row++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public virtual async Task<byte[]> ExportRevenueAsync(DateTime? from, DateTime? to)
        {
            var queryable = await _orders.GetQueryableAsync();
            var startUtc = from.HasValue ? VietnamTime.DayStartUtc(from.Value) : DateTime.MinValue;
            var endUtc = to.HasValue ? VietnamTime.DayEndExclusiveUtc(to.Value) : DateTime.MaxValue;
            var ranged = queryable.Where(x => x.CreatedAt >= startUtc && x.CreatedAt < endUtc);
            var billable = ranged.Where(x => x.Status != OrderStatus.Cancelled);

            var daily = await _asyncExecuter.ToListAsync(
                billable.GroupBy(x => x.CreatedAt.AddHours(7).Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        OrderCount = g.LongCount(),
                        Revenue = g.Sum(x => (decimal?)x.TotalAmount.Amount) ?? 0m
                    })
                    .OrderBy(x => x.Date));

            var details = await _asyncExecuter.ToListAsync(
                ranged.OrderByDescending(x => x.CreatedAt).Take(MaxExportRows));

            using var workbook = new XLWorkbook();

            // ---- Sheet 1: Tổng hợp theo ngày ----
            var summary = workbook.Worksheets.Add("Tổng hợp");
            var sumHeaders = new[] { "Ngày", "Số đơn", "Doanh thu (₫)", "Giá trị TB/đơn (₫)" };
            for (var c = 0; c < sumHeaders.Length; c++)
                summary.Cell(1, c + 1).Value = sumHeaders[c];
            StyleHeader(summary.Row(1));

            var r = 2;
            foreach (var d in daily)
            {
                summary.Cell(r, 1).Value = d.Date;
                summary.Cell(r, 1).Style.NumberFormat.Format = "dd/MM/yyyy";
                summary.Cell(r, 2).Value = d.OrderCount;
                summary.Cell(r, 3).Value = d.Revenue;
                summary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
                summary.Cell(r, 4).Value = d.OrderCount > 0 ? d.Revenue / d.OrderCount : 0m;
                summary.Cell(r, 4).Style.NumberFormat.Format = "#,##0";
                r++;
            }
            // Total row
            var totalOrders = daily.Sum(x => x.OrderCount);
            var totalRevenue = daily.Sum(x => x.Revenue);
            summary.Cell(r, 1).Value = "TỔNG CỘNG";
            summary.Cell(r, 2).Value = totalOrders;
            summary.Cell(r, 3).Value = totalRevenue;
            summary.Cell(r, 3).Style.NumberFormat.Format = "#,##0";
            summary.Cell(r, 4).Value = totalOrders > 0 ? totalRevenue / totalOrders : 0m;
            summary.Cell(r, 4).Style.NumberFormat.Format = "#,##0";
            summary.Row(r).Style.Font.Bold = true;
            summary.Columns().AdjustToContents();

            // ---- Sheet 2: Chi tiết đơn ----
            var sheet = workbook.Worksheets.Add("Chi tiết đơn");
            var headers = new[]
            {
                "Mã đơn", "Ngày đặt", "Khách hàng", "SĐT",
                "Món", "Tổng tiền", "Kênh thanh toán", "Trạng thái đơn"
            };
            for (var c = 0; c < headers.Length; c++)
                sheet.Cell(1, c + 1).Value = headers[c];
            StyleHeader(sheet.Row(1));

            var row = 2;
            foreach (var order in details)
            {
                var itemsText = string.Join("; ",
                    order.Lines.Select(x =>
                        $"{x.Quantity}× {x.ProductName}" +
                        (string.IsNullOrWhiteSpace(x.SizeName) ? "" : $" ({x.SizeName})")));
                sheet.Cell(row, 1).Value = order.OrderNumber;
                sheet.Cell(row, 2).Value = order.CreatedAt.AddHours(7);
                sheet.Cell(row, 2).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
                sheet.Cell(row, 3).Value = order.CustomerName;
                sheet.Cell(row, 4).Value = order.CustomerPhone;
                sheet.Cell(row, 5).Value = itemsText;
                sheet.Cell(row, 6).Value = order.TotalAmount.Amount;
                sheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                sheet.Cell(row, 7).Value = OrderLabels.PaymentMethod(order.PaymentMethod);
                sheet.Cell(row, 8).Value = OrderLabels.OrderStatus(order.Status);
                row++;
            }
            sheet.Columns().AdjustToContents();

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

        private static void StyleHeader(IXLRow headerRow)
        {
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#7a6fbe");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        private static IQueryable<Order> ApplyFilters(IQueryable<Order> query, GetOrdersInput input)
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
    /// Vietnamese labels for order enums (export, reconciliation).
    /// </summary>
    internal static class OrderLabels
    {
        public static string PaymentMethod(PaymentMethod method) => method switch
        {
            Domain.Entities.PaymentMethod.Cod => "COD",
            Domain.Entities.PaymentMethod.BankTransfer => "Chuyển khoản",
            Domain.Entities.PaymentMethod.MoMo => "MoMo",
            Domain.Entities.PaymentMethod.Wallet => "Ví",
            _ => method.ToString()
        };

        public static string PaymentStatus(PaymentStatus status) => status switch
        {
            Domain.Entities.PaymentStatus.Unpaid => "Chưa thanh toán",
            Domain.Entities.PaymentStatus.PendingConfirmation => "Chờ xác nhận",
            Domain.Entities.PaymentStatus.Paid => "Đã thanh toán",
            Domain.Entities.PaymentStatus.Failed => "Thất bại",
            _ => status.ToString()
        };

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
