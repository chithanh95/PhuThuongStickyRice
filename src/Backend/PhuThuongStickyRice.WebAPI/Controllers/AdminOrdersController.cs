using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Wave 6c (items 31–32): admin-only order reports —
    /// daily payment-channel reconciliation and Excel export.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/orders")]
    [ApiController]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderReportsAppService _reports;
        private readonly IOrderForecastAppService _forecast;

        public AdminOrdersController(
            IOrderReportsAppService reports,
            IOrderForecastAppService forecast)
        {
            _reports = reports;
            _forecast = forecast;
        }

        /// <summary>
        /// Wave 6c (item 31): per-payment-channel reconciliation of one
        /// Vietnam day. Omits <c>date</c> for today.
        /// </summary>
        [HttpGet("reconciliation")]
        public async Task<ActionResult> GetReconciliation(
            [FromQuery] DateTime? date,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _reports.GetDailyReconciliationAsync(date));
        }

        /// <summary>
        /// Wave 6c (item 32): exports the orders matching the given filters
        /// (search / status / shipper / date range) as an .xlsx file.
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult> Export(
            [FromQuery] string searchTerm = null,
            [FromQuery] string status = null,
            [FromQuery] Guid? shipperId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var bytes = await _reports.ExportOrdersAsync(new GetOrdersInput
            {
                MaxResultCount = 5000,
                SearchTerm = searchTerm,
                Status = status,
                ShipperId = shipperId,
                FromDate = fromDate,
                ToDate = toDate
            });

            // Filename like don-hang-2026-10-14.xlsx: prefer the filtered
            // range end, else the start, else today (Vietnam time).
            var day = toDate?.Date ?? fromDate?.Date ?? DateTime.UtcNow.AddHours(7).Date;
            var fileName = $"don-hang-{day:yyyy-MM-dd}.xlsx";

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// Revenue report for a Vietnam date range: sheet 1 = daily summary
        /// (orders, revenue, average order value), sheet 2 = order details.
        /// </summary>
        [HttpGet("export-revenue")]
        public async Task<ActionResult> ExportRevenue(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var bytes = await _reports.ExportRevenueAsync(fromDate, toDate);
            var from = (fromDate?.Date ?? DateTime.UtcNow.AddHours(7).Date).ToString("yyyy-MM-dd");
            var to = (toDate?.Date ?? DateTime.UtcNow.AddHours(7).Date).ToString("yyyy-MM-dd");
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"bao-cao-doanh-thu-{from}_{to}.xlsx");
        }
        /// <summary>
        /// Revenue forecast: per-weekday moving averages over recent
        /// history, next 7 days detail + next 30 days total.
        /// </summary>
        [HttpGet("revenue-forecast")]
        public async Task<ActionResult> GetRevenueForecast(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _forecast.GetRevenueForecastAsync());
        }
    }
}
