using PhuThuongStickyRice.Application.Orders;
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
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardAppService _dashboardAppService;

        public DashboardController(IDashboardAppService dashboardAppService)
        {
            _dashboardAppService = dashboardAppService;
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
    }
}
