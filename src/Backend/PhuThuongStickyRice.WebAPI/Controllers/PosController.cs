

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
        // Bug-hunt 2026-10-19: PosAppService (Item 127) là "counter POS for
        // Admin/Cashier" — controller để Admin-only thì thu ngân gọi
        // POST api/pos/orders bị 403 ngay ở cổng, không bao giờ tới service.
        [Authorize(Roles = "Admin,Cashier")]
        /// <summary>
        /// API bán hàng tại quán (POS): nhân viên tạo đơn cho khách đến mua
        /// trực tiếp — thanh toán tiền mặt, nhận hàng ngay, đơn hoàn tất luôn.
        /// Admin và Thu ngân (Cashier) được gọi.
        /// </summary>
        public class PosController : ControllerBase
        {
            private readonly IPosAppService _pos;

            /// <summary>Khởi tạo controller POS.</summary>
            /// <param name="pos">AppService tạo đơn bán hàng tại quán.</param>
            public PosController(IPosAppService pos)
            {
                _pos = pos;
            }

            /// <summary>
            /// Tạo đơn bán tại quán: COD (tiền mặt) + khách tự mang về,
            /// đơn được đánh dấu đã thanh toán và Hoàn tất ngay (không chờ giao).
            /// </summary>
            /// <param name="input">Thông tin đơn (món, số lượng, số tiền khách đưa...).</param>
            /// <param name="cancellationToken">Token hủy request.</param>
            /// <returns>Đơn hàng đã hoàn tất — in hóa đơn tại /in-hoa-don.</returns>
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
