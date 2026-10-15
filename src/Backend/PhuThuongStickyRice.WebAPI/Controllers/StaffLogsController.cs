using PhuThuongStickyRice.Application.Staff;
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
    /// Item 156: staff activity log viewer (Admin only).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/admin/staff-logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class StaffLogsController : ControllerBase
    {
        private readonly IStaffActivityLogAppService _logs;

        public StaffLogsController(IStaffActivityLogAppService logs)
        {
            _logs = logs;
        }

        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] Guid? userId,
            [FromQuery] string action,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int skipCount = 0,
            [FromQuery] int maxResultCount = 50,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _logs.GetListAsync(new GetStaffActivityLogsInput
            {
                UserId = userId,
                Action = action,
                From = from,
                To = to,
                SkipCount = skipCount,
                MaxResultCount = maxResultCount,
            }));
        }
    }
}
