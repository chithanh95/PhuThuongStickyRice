using PhuThuongStickyRice.Application.Orders;

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Inventory;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API thống kê dashboard: tổng quan doanh thu/đơn hàng, top món bán chạy,
    /// khách mới/quay lại, chỉ tiêu ngày và so sánh hôm nay với hôm qua.
    /// Endpoint today-sold mở public cho trang shop/landing ("Đã bán N gói hôm nay").
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin,Cashier")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardAppService _dashboardAppService;
        private readonly ICorporateClientAppService _corporateClients;
        private readonly IIngredientForecastAppService _forecast;

        public DashboardController(
            IDashboardAppService dashboardAppService,
            ICorporateClientAppService corporateClients,
            IIngredientForecastAppService forecast)
        {
            _dashboardAppService = dashboardAppService;
            _corporateClients = corporateClients;
            _forecast = forecast;
        }

        /// <summary>
        /// Thống kê tổng quan dashboard (doanh thu, số đơn, giá trị đơn trung
        /// bình...) trong khoảng thời gian — số liệu hiển thị trên trang admin.
        /// </summary>
        /// <param name="from">Từ ngày (query, tùy chọn).</param>
        /// <param name="to">Đến ngày (query, tùy chọn).</param>
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var stats = await _dashboardAppService.GetStatsAsync(from, to);
            return Ok(stats);
        }

        /// <summary>
        /// Top món bán chạy nhất trong khoảng thời gian (mặc định top 5) —
        /// hiển thị bảng "món bán chạy" trên dashboard.
        /// </summary>
        /// <param name="from">Từ ngày (query, tùy chọn).</param>
        /// <param name="to">Đến ngày (query, tùy chọn).</param>
        /// <param name="top">Số món lấy (query, mặc định 5).</param>
        [HttpGet("top-products")]
        public async Task<ActionResult> GetTopProducts(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int top = 5,
            CancellationToken cancellationToken = default)
        {
            var products = await _dashboardAppService.GetTopProductsAsync(from, to, top);
            return Ok(products);
        }

        /// <summary>
        /// Public: total quantity of items sold today (Vietnam time), shown
        /// on the shop hero and landing page ("Đã bán N gói hôm nay").
        /// </summary>
        [AllowAnonymous]
        [HttpGet("today-sold")]
        public async Task<ActionResult> GetTodaySold(CancellationToken cancellationToken = default)
        {
            var count = await _dashboardAppService.GetTodaySoldCountAsync();
            return Ok(new { quantitySold = count });
        }

        /// <summary>
        /// Item 90: new customers (first order inside the range) vs
        /// returning customers, over the selected date range.
        /// </summary>
        [HttpGet("customers/new-vs-returning")]
        public async Task<ActionResult> GetNewAndReturningCustomers(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetNewAndReturningCustomersAsync(from, to));
        }

        /// <summary>
        /// Item 139: today's revenue vs the daily target (Vietnam date).
        /// </summary>
        [HttpGet("daily-target")]
        public async Task<ActionResult> GetDailyTarget(CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetDailyTargetAsync());
        }

        /// <summary>
        /// Item 133: orders + revenue grouped by order source over a range.
        /// </summary>
        [HttpGet("source-stats")]
        public async Task<ActionResult> GetSourceStats(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetSourceStatsAsync(from, to));
        }

        /// <summary>
        /// Wave 6d (item 36): today's vs yesterday's revenue and order count
        /// (Vietnam day boundaries), with delta percents.
        /// </summary>
        [HttpGet("today-vs-yesterday")]
        public async Task<ActionResult> GetTodayVsYesterday(CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetTodayVsYesterdayAsync());
        }

        /// <summary>
        /// Item 122: corporate clients whose standing order falls on today's
        /// weekday (admin converts to a real order manually).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("corporate-due")]
        public async Task<ActionResult> GetCorporateDue(CancellationToken cancellationToken = default)
        {
            return Ok(await _corporateClients.GetDueTodayAsync());
        }

        /// <summary>
        /// Item 124: nếp forecast for tomorrow morning (dashboard widget).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("ingredient-forecast")]
        public async Task<ActionResult> GetIngredientForecast(CancellationToken cancellationToken = default)
        {
            return Ok(await _forecast.GetIngredientForecastAsync());
        }
    }
}
