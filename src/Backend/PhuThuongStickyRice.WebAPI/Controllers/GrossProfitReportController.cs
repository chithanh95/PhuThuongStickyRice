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
    /// Báo cáo lợi nhuận gộp từng món (Admin): dùng chung prefix
    /// "api/admin/reports" với AdminReportsController nhưng route action
    /// không trùng nhau nên không xung đột.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/reports")]
    [ApiController]
    public class GrossProfitReportController : ControllerBase
    {
        private readonly IGrossProfitReportAppService _grossProfit;

        /// <summary>Khởi tạo controller báo cáo lợi nhuận gộp.</summary>
        /// <param name="grossProfit">AppService tính lợi nhuận gộp theo giá vốn hiện tại của từng món.</param>
        public GrossProfitReportController(IGrossProfitReportAppService grossProfit)
        {
            _grossProfit = grossProfit;
        }

        /// <summary>
        /// Lợi nhuận gộp từng món theo khoảng ngày Việt Nam (mặc định 7 ngày
        /// gần nhất). Giá vốn tính theo CostPrice hiện tại của món.
        /// </summary>
        [HttpGet("gross-profit")]
        public async Task<ActionResult> GetGrossProfit(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _grossProfit.GetGrossProfitReportAsync(from, to));
        }

        /// <summary>Tất cả món kèm giá vốn hiện tại — dùng cho màn sửa giá vốn.</summary>
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

        /// <summary>Đặt giá vốn (CostPrice) của 1 món.</summary>
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
