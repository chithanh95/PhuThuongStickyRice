

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
        /// API điểm danh nhận điểm loyalty (public): khách điểm danh mỗi ngày
        /// 1 lần để nhận điểm thưởng, và tra cứu trạng thái điểm danh + số
        /// ngày liên tiếp (streak) hiện tại theo SĐT.
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

            /// <summary>Public: daily check-in; awards loyalty points once per day.</summary>
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
                    // Lỗi nghiệp vụ (SĐT sai, dữ liệu không hợp lệ...) → 400
                    // thay vì để lọt thành 500.
                    return BadRequest(new { message = ex.Message });
                }
            }

            /// <summary>Public: today's check-in state + streak for a phone.</summary>
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
