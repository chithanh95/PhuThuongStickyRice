using PhuThuongStickyRice.Application.Reports;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 138: admin-only weekly owner report — view the aggregated
    /// numbers and trigger a Zalo OA delivery manually. The automatic
    /// Monday 08:00 send runs in the BackgroundServer worker.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/reports")]
    [ApiController]
    public class AdminReportsController : ControllerBase
    {
        private readonly IWeeklyReportAppService _weeklyReport;

        public AdminReportsController(IWeeklyReportAppService weeklyReport)
        {
            _weeklyReport = weeklyReport;
        }

        /// <summary>Aggregated numbers for the last full Monday–Sunday week.</summary>
        [HttpGet("weekly")]
        public async Task<ActionResult<WeeklyReportDto>> GetWeekly()
        {
            return Ok(await _weeklyReport.GetWeeklyReportAsync());
        }

        /// <summary>
        /// Builds the weekly report and sends it via Zalo OA when
        /// configured; otherwise returns the text so the owner can copy
        /// and send it manually. Also records the idempotency marker so
        /// the Monday worker won't resend the same week.
        /// </summary>
        [HttpPost("weekly/send")]
        public async Task<ActionResult<WeeklyReportSendResultDto>> SendWeekly()
        {
            return Ok(await _weeklyReport.SendWeeklyReportAsync());
        }
    }
}
