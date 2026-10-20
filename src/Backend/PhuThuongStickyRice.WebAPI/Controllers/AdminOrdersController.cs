using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API quản trị đơn hàng: đối soát, xuất báo cáo Excel, dự báo doanh thu,
    /// doanh số theo giờ và thu ngân xác nhận "Đã thu tiền" (Cashier).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    // Bug-hunt 2026-10-19: class-level mở cho Admin,Cashier để
    // MarkCashCollected (thu ngân "Đã thu tiền") qua được — ASP.NET Core gộp
    // nhiều [Authorize] bằng AND nên [Authorize(Roles="Admin")] ở class +
    // [Authorize(Roles="Admin,Cashier")] ở method vẫn chặn Cashier (403).
    // 7 báo cáo bên dưới giữ Admin-only bằng attribute riêng từng method.
    [Authorize(Roles = "Admin,Cashier")]
    [Route("api/admin/orders")]
    [ApiController]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderReportsAppService _reports;
        private readonly IOrderForecastAppService _forecast;
        private readonly IOrderHourlyReportAppService _hourlyReport;
        private readonly IOrderAppService _orderAppService;

        public AdminOrdersController(
            IOrderReportsAppService reports,
            IOrderForecastAppService forecast,
            IOrderHourlyReportAppService hourlyReport,
            IOrderAppService orderAppService)
        {
            _reports = reports;
            _forecast = forecast;
            _hourlyReport = hourlyReport;
            _orderAppService = orderAppService;
        }

        /// <summary>
        /// Wave 6c (item 31): per-payment-channel reconciliation of one
        /// Vietnam day. Omits <c>date</c> for today.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("reconciliation")]
        public async Task<ActionResult> GetReconciliation(
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _reports.GetDailyReconciliationAsync(date));
        }

        /// <summary>
        /// Wave 6c (item 32): exports the orders matching the given filters
        /// (search / status / shipper / date range / cash-collected / VIP-only)
        /// as an .xlsx file.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("export")]
        public async Task<ActionResult> Export(
            [FromQuery] string searchTerm = null,
            [FromQuery] string status = null,
            [FromQuery] Guid? shipperId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool? cashCollected = null,
            [FromQuery] bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            var bytes = await _reports.ExportOrdersAsync(new GetOrdersInput
            {
                MaxResultCount = 5000,
                SearchTerm = searchTerm,
                Status = status,
                ShipperId = shipperId,
                FromDate = fromDate,
                ToDate = toDate,
                CashCollected = cashCollected,
                VipOnly = vipOnly
            });

            // Filename like don-hang-2026-10-14.xlsx: prefer the filtered
            // range end, else the start, else today (Vietnam time).
            var day = toDate?.Date ?? fromDate?.Date ?? VietnamTime.Today();
            var fileName = $"don-hang-{day:yyyy-MM-dd}.xlsx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Revenue report for a Vietnam date range: sheet 1 = daily summary
        /// (orders, revenue, average order value), sheet 2 = order details.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("export-revenue")]
        public async Task<ActionResult> ExportRevenue(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var bytes = await _reports.ExportRevenueAsync(fromDate, toDate);
            var fromText = (fromDate?.Date ?? VietnamTime.Today()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toText = (toDate?.Date ?? VietnamTime.Today()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"bao-cao-doanh-thu-{fromText}_{toText}.xlsx");
        }
        /// <summary>
        /// Revenue forecast: per-weekday moving averages over recent
        /// history, next 7 days detail + next 30 days total.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("revenue-forecast")]
        public async Task<ActionResult> GetRevenueForecast(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _forecast.GetRevenueForecastAsync());
        }

        /// <summary>
        /// Hourly best-sellers report for one Vietnam-local day
        /// (heatmap source: hours 6h–22h × products).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("hourly-sales")]
        public async Task<ActionResult> GetHourlySales(
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _hourlyReport.GetHourlySalesReportAsync(date));
        }

        /// <summary>
        /// Item 169: revenue summary of one Vietnam date range (cancelled
        /// orders excluded) — order count, total revenue, average order
        /// value, top 5 products. The compare view calls this once per
        /// period.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("revenue-summary")]
        public async Task<ActionResult> GetRevenueSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _reports.GetRevenueSummaryAsync(from, to));
        }

        /// <summary>
        /// Item 142: groups of possibly-duplicate orders (same phone,
        /// placed within the last 30 minutes, still Pending/Confirmed)
        /// for the admin review panel. Never merged automatically.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("duplicates")]
        public async Task<ActionResult> GetDuplicates(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _orderAppService.GetDuplicateOrderGroupsAsync());
        }

        /// <summary>
        /// Item 163: staff marks cash as collected ("💵 Đã thu tiền").
        /// Method-level authorize widens the class-level Admin-only rule
        /// to Cashiers as well.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("{id:guid}/mark-cash-collected")]
        public async Task<ActionResult> MarkCashCollected(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _orderAppService.MarkCashCollectedAsync(id));
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
