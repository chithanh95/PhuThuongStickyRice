using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Zalo
{
    public interface IZaloNotificationService
    {
        /// <summary>Sends (or stubs) a Zalo OA e-receipt for an order.</summary>
        Task SendOrderReceiptAsync(string orderNumber, string customerPhone, decimal totalAmount);

        /// <summary>Sends (or stubs) a plain text message via Zalo OA.</summary>
        Task SendTextMessageAsync(string phone, string message);
    }

    /// <summary>
    /// Zalo OA integration — STUB. No real HTTP call is made: with the
    /// default "xxx" placeholders the service only logs what it WOULD send.
    /// To go live, fill in <see cref="ZaloOaOptions"/> and implement the
    /// ZNS/template-message call inside <see cref="SendTextMessageAsync"/>.
    /// </summary>
    public class ZaloNotificationService : IZaloNotificationService
    {
        private readonly ZaloOaOptions _options;
        private readonly ILogger<ZaloNotificationService> _logger;

        public ZaloNotificationService(
            ZaloOaOptions options,
            ILogger<ZaloNotificationService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task SendOrderReceiptAsync(string orderNumber, string customerPhone, decimal totalAmount)
        {
            var message =
                $"Xôi Phú Thượng: đơn hàng {orderNumber} ({totalAmount:N0} ₫) đã được ghi nhận. " +
                "Cảm ơn quý khách!";
            return SendTextMessageAsync(customerPhone, message);
        }

        public Task SendTextMessageAsync(string phone, string message)
        {
            if (!_options.IsConfigured)
            {
                _logger.LogInformation(
                    "[Zalo OA stub] would send Zalo message to {Phone} via OA {OaId}: {Message}",
                    phone,
                    _options.OaId,
                    message);
                return Task.CompletedTask;
            }

            // TODO (go-live): POST to https://openapi.zalo.me/v3.0/oa/message/cs
            // with the access token, mapping the phone to the OA user id first
            // (Zalo identifies recipients by userId, not phone).
            _logger.LogWarning(
                "[Zalo OA] configured but the real send is not implemented yet. " +
                "Would send to {Phone}: {Message}",
                phone,
                message);
            return Task.CompletedTask;
        }
    }
}
