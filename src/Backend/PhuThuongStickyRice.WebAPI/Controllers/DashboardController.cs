using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Wave5;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
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

        [HttpGet("stats")]
        public async Task<ActionResult> GetStats(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var stats = await _dashboardAppService.GetStatsAsync(from, to);
            return Ok(stats);
        }

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
        public async Task<ActionResult> GetNewReturningCustomers(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _dashboardAppService.GetNewReturningCustomersAsync(from, to));
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
            return Ok(await _forecast.GetForecastAsync());
        }
    }
}
