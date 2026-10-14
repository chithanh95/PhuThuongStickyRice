using PhuThuongStickyRice.Application.Wave3;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 84: profit report — revenue − cost = gross profit (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfitReportController : ControllerBase
    {
        private readonly IProfitReportAppService _profitReport;

        public ProfitReportController(IProfitReportAppService profitReport)
        {
            _profitReport = profitReport;
        }

        [HttpGet]
        public async Task<ActionResult> GetReport(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profitReport.GetReportAsync(from, to));
        }
    }
}
