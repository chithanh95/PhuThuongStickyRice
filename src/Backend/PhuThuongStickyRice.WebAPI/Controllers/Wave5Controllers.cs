using PhuThuongStickyRice.Application.Wave5;
using PhuThuongStickyRice.Application.Wave5.DTOs;
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

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    // ================= Check-in (item 121) =================

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

    public class CheckInRequestModel
    {
        public string Phone { get; set; }
    }

    // ================= Corporate clients (item 122) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ForecastController : ControllerBase
    {
        private readonly IIngredientForecastAppService _forecast;

        public ForecastController(IIngredientForecastAppService forecast)
        {
            _forecast = forecast;
        }

        [HttpGet]
        public async Task<ActionResult<IngredientForecastDto>> Get(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _forecast.GetForecastAsync());
        }
    }

    // ================= Work shifts (item 125) =================

    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletAppService _wallets;

        public WalletsController(IWalletAppService wallets)
        {
            _wallets = wallets;
        }

        /// <summary>Public: balance for a phone (checkout shows the ví option when ≥ total).</summary>
        [AllowAnonymous]
        [HttpGet("balance")]
        public async Task<ActionResult<WalletAccountDto>> GetBalance(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _wallets.GetBalanceAsync(phone));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<List<WalletAccountDto>>> GetList(
            [FromQuery] string search,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _wallets.GetListAsync(search));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("transactions")]
        public async Task<ActionResult<List<WalletTransactionDto>>> GetTransactions(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _wallets.GetTransactionsAsync(phone));
        }

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

    // ================= Shift close (item 129) =================

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

    // ================= Message templates (item 131) =================

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

    // ================= MoMo reconciliation (item 128) =================

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
