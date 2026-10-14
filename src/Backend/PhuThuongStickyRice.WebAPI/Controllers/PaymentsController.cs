using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Payment endpoints: public bank-transfer info + MoMo callback.
    /// (MoMo create lives on OrdersController: POST /api/orders/{id}/momo/create.)
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly BankTransferOptions _bankTransfer;
        private readonly IMoMoPaymentService _momo;

        public PaymentsController(
            BankTransferOptions bankTransfer,
            IMoMoPaymentService momo)
        {
            _bankTransfer = bankTransfer;
            _momo = momo;
        }

        /// <summary>Public bank-transfer details shown at checkout (account info + VietQR).</summary>
        [AllowAnonymous]
        [HttpGet("bank-transfer-info")]
        public ActionResult<BankTransferInfoDto> GetBankTransferInfo()
        {
            return Ok(new BankTransferInfoDto
            {
                BankName = _bankTransfer.BankName,
                BankId = _bankTransfer.BankId,
                AccountNumber = _bankTransfer.AccountNumber,
                AccountName = _bankTransfer.AccountName,
                QrTemplate = _bankTransfer.QrTemplate
            });
        }

        /// <summary>
        /// MoMo return / IPN. The real MoMo posts a signed IPN; the TEST
        /// simulator posts the echoed create-signature. Both are verified.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("momo/callback")]
        public async Task<ActionResult> MoMoCallback([FromBody] MoMoCallbackDto callback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _momo.HandleCallbackAsync(callback);
                return Ok(new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    paymentStatus = order.PaymentStatus.ToString()
                });
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
