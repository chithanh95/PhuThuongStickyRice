

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
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
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
