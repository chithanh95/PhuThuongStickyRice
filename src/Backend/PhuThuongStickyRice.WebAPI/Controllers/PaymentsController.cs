using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Application.Wave4;
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
        private readonly IPaymentConfigProvider _paymentConfig;
        private readonly IMoMoPaymentService _momo;

        public PaymentsController(
            IPaymentConfigProvider paymentConfig,
            IMoMoPaymentService momo)
        {
            _paymentConfig = paymentConfig;
            _momo = momo;
        }

        /// <summary>
        /// Public bank-transfer details shown at checkout (account info + VietQR).
        /// Item 106: admin-editable at /admin/thanh-toan (ShopSetting), falling
        /// back to appsettings.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("bank-transfer-info")]
        public async Task<ActionResult<BankTransferInfoDto>> GetBankTransferInfo(
            CancellationToken cancellationToken = default)
        {
            var bankTransfer = await _paymentConfig.GetBankTransferAsync();
            return Ok(new BankTransferInfoDto
            {
                BankName = bankTransfer.BankName,
                BankId = bankTransfer.BankId,
                AccountNumber = bankTransfer.AccountNumber,
                AccountName = bankTransfer.AccountName,
                QrTemplate = bankTransfer.QrTemplate
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
