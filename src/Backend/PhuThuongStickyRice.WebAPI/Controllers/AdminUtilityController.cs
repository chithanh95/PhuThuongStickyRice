using PhuThuongStickyRice.Application.Admin;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Items 157–158: admin global search + notification center.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin,Cashier")]
    [Route("api/admin")]
    [ApiController]
    public class AdminUtilityController : ControllerBase
    {
        private readonly IAdminUtilityAppService _utility;

        public AdminUtilityController(IAdminUtilityAppService utility)
        {
            _utility = utility;
        }

        /// <summary>
        /// Item 157: global search across orders, products, customers.
        /// Minimum 2 characters; results are capped at 5 per group.
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<GlobalSearchResultDto>> Search(
            [FromQuery] string q,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _utility.SearchAsync(q));
        }

        /// <summary>
        /// Item 158: aggregated notifications — pending/stale orders,
        /// upcoming pre-orders, new 1–2 star reviews, today's birthdays.
        /// </summary>
        [HttpGet("notifications")]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _utility.GetNotificationsAsync());
        }
    }
}
