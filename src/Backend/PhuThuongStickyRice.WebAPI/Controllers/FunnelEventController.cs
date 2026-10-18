using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhuThuongStickyRice.Application.Reports;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// GL1: anonymous funnel measurement (landing/shop/cart/checkout/order/track views).
    /// No PII is stored — event names are allowlisted, URLs are path-only.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/funnel-events")]
    [ApiController]
    public class FunnelEventController : ControllerBase
    {
        private readonly IFunnelEventAppService _funnel;

        public FunnelEventController(IFunnelEventAppService funnel)
        {
            _funnel = funnel;
        }

        /// <summary>Public: record one funnel event (fire-and-forget from the web).</summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Track(
            [FromBody] TrackFunnelEventDto input,
            CancellationToken cancellationToken = default)
        {
            await _funnel.TrackAsync(input ?? new TrackFunnelEventDto());
            return Ok(new { success = true });
        }

        /// <summary>Admin: funnel counts per event — today + last 7 days.</summary>
        [HttpGet("report")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetReport()
        {
            var report = await _funnel.GetReportAsync();
            return Ok(report);
        }
    }
}
