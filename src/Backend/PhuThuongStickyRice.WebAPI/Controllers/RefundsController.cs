

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
        /// <summary>
        /// API hoàn tiền cho đơn hàng (Item 130). Controller này chỉ ghi nhận
        /// trạng thái yêu cầu/xác nhận trên hệ thống — tiền thật do nhân viên
        /// chuyển khoản tay cho khách. Chỉ Admin/Cashier được gọi.
        /// </summary>
        public class RefundsController : ControllerBase
        {
            private readonly IRefundAppService _refunds;

            public RefundsController(IRefundAppService refunds)
            {
                _refunds = refunds;
            }

            /// <summary>
            /// Báo cáo hoàn tiền: các đơn đã yêu cầu hoàn hoặc đã hoàn tiền xong,
            /// đơn mới nhất trước (tối đa 500 dòng).
            /// </summary>
            [HttpGet]
            public async Task<ActionResult> GetReport(CancellationToken cancellationToken = default)
            {
                return Ok(await _refunds.GetReportAsync());
            }

            /// <summary>
            /// Ghi nhận yêu cầu hoàn tiền cho một đơn (kèm số tiền và lý do).
            /// Lỗi nghiệp vụ (DomainException) trả 400 với message thân thiện.
            /// </summary>
            /// <param name="orderId">Id đơn hàng cần hoàn tiền.</param>
            /// <param name="input">Số tiền và lý do hoàn tiền.</param>
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

            /// <summary>
            /// Xác nhận đã chuyển tiền hoàn cho khách (gọi sau khi chuyển khoản tay xong).
            /// Lỗi nghiệp vụ (DomainException) trả 400 với message thân thiện.
            /// </summary>
            /// <param name="orderId">Id đơn hàng đã hoàn tiền.</param>
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
