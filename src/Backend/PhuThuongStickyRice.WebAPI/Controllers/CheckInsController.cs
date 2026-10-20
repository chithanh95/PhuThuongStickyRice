

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API điểm danh hàng ngày: khách điểm danh bằng SĐT để cộng điểm
    /// loyalty (mỗi SĐT chỉ được cộng 1 lần/ngày) và theo dõi chuỗi ngày
    /// điểm danh liên tiếp để nhận thưởng.
    /// </summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class CheckInsController : ControllerBase
        {
            private readonly ICheckInAppService _checkIns;

            public CheckInsController(ICheckInAppService checkIns)
            {
                _checkIns = checkIns;
            }

            /// <summary>
            /// Public: điểm danh hàng ngày bằng SĐT; mỗi SĐT chỉ được cộng
            /// điểm loyalty 1 lần mỗi ngày.
            /// </summary>
            [AllowAnonymous]
            [HttpPost]
            public async Task<ActionResult<CheckInResultDto>> CheckIn(
                [FromBody] CheckInRequestModel model,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    return Ok(await _checkIns.CheckInAsync(model?.Phone));
                }
                catch (DomainException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            /// <summary>
            /// Public: trạng thái điểm danh hôm nay + chuỗi ngày liên tiếp
            /// của một SĐT (để hiện "đã điểm danh"/"chưa điểm danh").
            /// </summary>
            [AllowAnonymous]
            [HttpGet("status")]
            public async Task<ActionResult<CheckInResultDto>> GetStatus(
                [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _checkIns.GetStatusAsync(phone));
            }
        }
}
