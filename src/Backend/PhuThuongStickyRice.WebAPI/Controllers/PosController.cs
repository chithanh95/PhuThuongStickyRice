

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
        [Authorize(Roles = "Admin")]
        public class PosController : ControllerBase
        {
            private readonly IPosAppService _pos;

            public PosController(IPosAppService pos)
            {
                _pos = pos;
            }

            /// <summary>
            /// Walk-in order: COD + pickup, paid and Completed immediately.
            /// Returns the finished order (print the receipt via /in-hoa-don).
            /// </summary>
            [HttpPost("orders")]
            public async Task<ActionResult> CreateOrder(
                [FromBody] CreatePosOrderInput input,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    return Ok(await _pos.CreateOrderAsync(input));
                }
                catch (DomainException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
        }
}
