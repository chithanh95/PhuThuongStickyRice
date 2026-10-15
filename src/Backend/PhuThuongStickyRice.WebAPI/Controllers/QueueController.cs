using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Public queue-number display for the waiting-area TV screen (/cho,
    /// item 178). Privacy-safe: only queue number, short code and coarse
    /// status — no phone, no name, no amounts.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/queue")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly IQueueDisplayAppService _queue;

        public QueueController(IQueueDisplayAppService queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Public: today's tickets — Confirmed/Preparing ("Đang chuẩn bị")
        /// and Delivering ("Mời lấy món").
        /// </summary>
        [AllowAnonymous]
        [HttpGet("now-serving")]
        public async Task<ActionResult> GetNowServing(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _queue.GetNowServingAsync());
        }
    }
}
