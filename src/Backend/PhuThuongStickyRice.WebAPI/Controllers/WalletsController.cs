

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
using PhuThuongStickyRice.Application.Loyalty;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>
        /// API ví khách hàng: khách xem số dư theo SĐT để checkout trừ vào ví;
        /// admin liệt kê ví, xem lịch sử giao dịch và cộng/trừ số dư thủ công.
        /// </summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        [Authorize(Roles = "Admin")]
        public class WalletsController : ControllerBase
        {
            private readonly IWalletAppService _wallets;

            /// <summary>Khởi tạo controller ví khách hàng.</summary>
            /// <param name="wallets">AppService quản lý số dư và giao dịch ví.</param>
            public WalletsController(IWalletAppService wallets)
            {
                _wallets = wallets;
            }

            /// <summary>
            /// Public: số dư ví theo SĐT — checkout hiện tùy chọn "trả bằng ví"
            /// khi số dư ≥ tổng tiền đơn.
            /// </summary>
            [AllowAnonymous]
            [HttpGet("balance")]
            public async Task<ActionResult<WalletAccountDto>> GetBalance(
                [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _wallets.GetBalanceAsync(phone));
            }

            /// <summary>
            /// Admin: danh sách ví khách hàng (tìm kiếm theo SĐT) — màn quản
            /// lý ví ở admin.
            /// </summary>
            [Authorize(Roles = "Admin")]
            [HttpGet]
            public async Task<ActionResult<List<WalletAccountDto>>> GetList(
                [FromQuery] string search,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _wallets.GetListAsync(search));
            }

            /// <summary>
            /// Admin: lịch sử giao dịch ví của 1 SĐT (nạp, thanh toán, hoàn tiền)
            /// — dùng khi đối soát khiếu nại số dư.
            /// </summary>
            [Authorize(Roles = "Admin")]
            [HttpGet("transactions")]
            public async Task<ActionResult<List<WalletTransactionDto>>> GetTransactions(
                [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _wallets.GetTransactionsAsync(phone));
            }

            /// <summary>
            /// Admin: cộng/trừ số dư ví thủ công (nạp bù, điều chỉnh sai lệch).
            /// Lỗi nghiệp vụ (ví dụ số dư không đủ khi trừ) trả 400.
            /// </summary>
            [Authorize(Roles = "Admin")]
            [HttpPost("adjust")]
            public async Task<ActionResult<WalletAccountDto>> Adjust(
                [FromBody] AdjustWalletInput input,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    return Ok(await _wallets.AdjustAsync(input));
                }
                catch (DomainException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
        }
}
