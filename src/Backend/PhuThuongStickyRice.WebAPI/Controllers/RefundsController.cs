

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
        [Authorize(Roles = "Admin,Cashier")]
        public class RefundsController : ControllerBase
        {
            private readonly IRefundAppService _refunds;

            public RefundsController(IRefundAppService refunds)
            {
                _refunds = refunds;
            }

            [HttpGet]
            public async Task<ActionResult> GetReport(CancellationToken cancellationToken = default)
            {
                return Ok(await _refunds.GetReportAsync());
            }

            [HttpPost("orders/{orderId:guid}/request")]
            public async Task<ActionResult> RequestRefund(Guid orderId,
                [FromBody] RequestRefundInput input,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    await _refunds.RequestRefundAsync(orderId, input ?? new RequestRefundInput());
                    return Ok(new { message = "Đã ghi nhận yêu cầu hoàn tiền." });
                }
                catch (DomainException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            [HttpPost("orders/{orderId:guid}/confirm")]
            public async Task<ActionResult> ConfirmRefund(Guid orderId,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    await _refunds.ConfirmRefundAsync(orderId);
                    return Ok(new { message = "Đã xác nhận hoàn tiền." });
                }
                catch (DomainException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
        }
}
