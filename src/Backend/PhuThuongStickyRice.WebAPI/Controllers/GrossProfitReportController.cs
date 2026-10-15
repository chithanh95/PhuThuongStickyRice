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
    /// <summary>
    /// Item 154: gross profit per product (Admin-only). Shares the
    /// "api/admin/reports" prefix with AdminReportsController — action
    /// routes do not collide.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/reports")]
    [ApiController]
    public class GrossProfitReportController : ControllerBase
    {
        private readonly IGrossProfitReportAppService _grossProfit;

        public GrossProfitReportController(IGrossProfitReportAppService grossProfit)
        {
            _grossProfit = grossProfit;
        }

        /// <summary>
        /// Gross profit per product for a Vietnam date range (defaults: last
        /// 7 days). Cost is valued at each product's current CostPrice.
        /// </summary>
        [HttpGet("gross-profit")]
        public async Task<ActionResult> GetGrossProfit(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _grossProfit.GetGrossProfitReportAsync(from, to));
        }

        /// <summary>All products with their current cost price (cost editor).</summary>
        [HttpGet("gross-profit/products")]
        public async Task<ActionResult> GetProductCosts(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _grossProfit.GetProductCostsAsync());
        }

        public class UpdateCostPriceRequest
        {
            public decimal CostPrice { get; set; }
        }

        /// <summary>Sets one product's cost price (giá vốn).</summary>
        [HttpPut("gross-profit/products/{id}/cost-price")]
        public async Task<ActionResult> UpdateCostPrice(
            Guid id,
            [FromBody] UpdateCostPriceRequest request,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _grossProfit.UpdateCostPriceAsync(id, request.CostPrice));
        }
    }
}
