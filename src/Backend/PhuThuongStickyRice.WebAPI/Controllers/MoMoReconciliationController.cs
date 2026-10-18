

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
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        [Authorize(Roles = "Admin")]
        public class MoMoReconciliationController : ControllerBase
        {
            private readonly IMoMoReconciliationAppService _reconciliation;

            public MoMoReconciliationController(IMoMoReconciliationAppService reconciliation)
            {
                _reconciliation = reconciliation;
            }

            /// <summary>MoMo-paid orders; filter by matched=true/false (omit for all).</summary>
            [HttpGet("orders")]
            public async Task<ActionResult<List<MoMoOrderDto>>> GetOrders(
                [FromQuery] bool? matched,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _reconciliation.GetMoMoOrdersAsync(matched));
            }

            /// <summary>Manual "đánh dấu đã đối soát".</summary>
            [HttpPost("orders/{orderId:guid}/mark-matched")]
            public async Task<ActionResult> MarkMatched(Guid orderId,
                CancellationToken cancellationToken = default)
            {
                await _reconciliation.MarkMatchedAsync(orderId);
                return Ok(new { message = "Đã đánh dấu đối soát." });
            }
        }
}
