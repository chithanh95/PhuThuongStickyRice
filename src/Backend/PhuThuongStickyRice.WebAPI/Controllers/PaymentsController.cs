using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Payments;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API thanh toán: thông tin chuyển khoản (public) + callback MoMo.
    /// (Khởi tạo thanh toán MoMo nằm ở OrdersController:
    /// POST /api/orders/{id}/momo/create.)
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentConfigProvider _paymentConfig;
        private readonly IMoMoPaymentService _momo;
        private readonly IFeatureFlagAppService _flags;

        /// <summary>Khởi tạo controller thanh toán.</summary>
        /// <param name="paymentConfig">Nguồn cấu hình chuyển khoản (ShopSetting trước, appsettings sau).</param>
        /// <param name="momo">Service xử lý thanh toán MoMo (tạo thanh toán + nhận callback).</param>
        /// <param name="flags">Cờ tính năng — chặn callback khi MoMo đang tắt.</param>
    public PaymentsController(
            IPaymentConfigProvider paymentConfig,
            IMoMoPaymentService momo,
            IFeatureFlagAppService flags)
        {
            // MoMo create nằm ở OrdersController: POST /api/orders/{id}/momo/create —
            // controller này chỉ nhận callback từ MoMo và cung cấp thông tin chuyển khoản.
            _paymentConfig = paymentConfig;
            _momo = momo;
            _flags = flags;
        }

        /// <summary>
        /// Thông tin chuyển khoản hiển thị ở checkout (số TK, tên TK, VietQR).
        /// Item 106: admin tự sửa ở /admin/thanh-toan (lưu vào ShopSetting),
        /// chưa cấu hình thì lấy từ appsettings.
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
        /// Callback MoMo: MoMo thật post IPN kèm chữ ký số; bản mô phỏng (test)
        /// post chữ ký lấy từ bước create. Cả hai đều được verify trong HandleCallbackAsync.
        /// Lỗi nghiệp vụ (DomainException) trả 400 để phía gọi biết xử lý.
        /// </summary>
        /// <param name="callback">Dữ liệu MoMo gửi về (đơn hàng, trạng thái, chữ ký).</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpPost("momo/callback")]
        public async Task<ActionResult> MoMoCallback([FromBody] MoMoCallbackDto callback,
            CancellationToken cancellationToken = default)
        {
            // User 2026-10-21: MoMo đang tắt (cờ "momo-payments", mặc định
            // TẮT) → chặn luôn callback để endpoint anonymous này không xác
            // nhận được thanh toán nào, kể cả callback giả lập TEST (chặn
            // chuỗi tạo đơn Paid giả đã ghi nhận ở bug-hunt 2026-10-19).
            if (!await _flags.IsEnabledAsync(FeatureFlag.MoMoPayments))
            {
                return NotFound();
            }

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
