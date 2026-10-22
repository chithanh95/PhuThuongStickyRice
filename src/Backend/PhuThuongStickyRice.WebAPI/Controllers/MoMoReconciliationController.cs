

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

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>
        /// Admin: đối soát thanh toán MoMo — đối chiếu đơn đã thanh toán MoMo
        /// với báo cáo đối soát (matched/unmatched) và đánh dấu thủ công khi lệch.
        /// </summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/momo-reconciliation")]
        [ApiController]
        [Authorize(Roles = "Admin")]
        public class MoMoReconciliationController : ControllerBase
        {
            private readonly IMoMoReconciliationAppService _reconciliation;

            public MoMoReconciliationController(IMoMoReconciliationAppService reconciliation)
            {
                _reconciliation = reconciliation;
            }

            /// <summary>
            /// Admin: đơn đã thanh toán MoMo để đối soát; lọc theo matched=true/false
            /// (bỏ qua để lấy tất cả). Đơn unmatched cần admin đối chiếu với báo cáo MoMo.
            /// </summary>
            [HttpGet("orders")]
            public async Task<ActionResult<List<MoMoOrderDto>>> GetOrders(
                [FromQuery] bool? matched,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _reconciliation.GetMoMoOrdersAsync(matched));
            }

            /// <summary>
            /// Admin: đánh dấu thủ công 1 đơn MoMo là "đã đối soát" — dùng khi
            /// tiền thực tế đã về nhưng đối soát tự động không khớp (lệch số
            /// tiền hoặc thiếu trong báo cáo MoMo).
            /// </summary>
            [HttpPost("orders/{orderId:guid}/mark-matched")]
            public async Task<ActionResult> MarkMatched(Guid orderId,
                CancellationToken cancellationToken = default)
            {
                await _reconciliation.MarkMatchedAsync(orderId);
                return Ok(new { message = "Đã đánh dấu đối soát." });
            }
        }
}
