using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Zalo
{
    public interface IZaloNotificationService
    {
        /// <summary>
        /// Gửi biên nhận điện tử của đơn hàng qua Zalo OA. Khi chưa cấu hình
        /// thật (OA chưa điền token) chỉ ghi log stub để không làm gãy luồng tạo đơn.
        /// </summary>
        /// <param name="orderNumber">Mã đơn hàng hiển thị trên biên nhận.</param>
        /// <param name="customerPhone">SĐT khách hàng nhận tin.</param>
        /// <param name="totalAmount">Tổng tiền đơn (đ).</param>
        Task SendOrderReceiptAsync(string orderNumber, string customerPhone, decimal totalAmount);

        /// <summary>
        /// Gửi tin nhắn văn bản thường qua Zalo OA (tin CSKH). Khi chưa cấu
        /// hình OA thật chỉ ghi log stub.
        /// </summary>
        /// <param name="phone">SĐT người nhận.</param>
        /// <param name="message">Nội dung tin nhắn.</param>
        Task SendTextMessageAsync(string phone, string message);
    }

    /// <summary>
    /// Tích hợp Zalo OA — ĐANG Ở DẠNG STUB, chưa gọi HTTP thật: khi option để
    /// placeholder "xxx" mặc định thì service chỉ ghi log nội dung MUỐN gửi.
    /// Muốn go-live: điền <see cref="ZaloOaOptions"/> và cài đặt gọi ZNS /
    /// tin nhắn mẫu trong <see cref="SendTextMessageAsync"/>.
    /// </summary>
    public class ZaloNotificationService : IZaloNotificationService
    {
        private readonly ZaloOaOptions _options;
        private readonly ILogger<ZaloNotificationService> _logger;

        // CA1848: dùng LoggerMessage delegate thay LoggerExtensions.LogInformation/
        // LogWarning — delegate biên dịch sẵn template, không box params và không
        // parse template mỗi lần gọi. Nội dung log giữ nguyên.
        private static readonly Action<ILogger, string, string, string, Exception?> LogZaloStub =
            LoggerMessage.Define<string, string, string>(
                LogLevel.Information,
                new EventId(1, "ZaloStub"),
                "[Zalo OA stub] would send Zalo message to {Phone} via OA {OaId}: {Message}");

        private static readonly Action<ILogger, string, string, Exception?> LogZaloNotImplemented =
            LoggerMessage.Define<string, string>(
                LogLevel.Warning,
                new EventId(2, "ZaloNotImplemented"),
                "[Zalo OA] configured but the real send is not implemented yet. Would send to {Phone}: {Message}");

        /// <summary>
        /// Khởi tạo service gửi tin Zalo OA với cấu hình OA và logger ghi stub.
        /// </summary>
        public ZaloNotificationService(
            ZaloOaOptions options,
            ILogger<ZaloNotificationService> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Gửi biên nhận điện tử cho 1 đơn hàng: soạn nội dung rồi dùng chung
        /// <see cref="SendTextMessageAsync"/> để gửi (hoặc log stub).
        /// </summary>
        /// <param name="orderNumber">Mã đơn hàng.</param>
        /// <param name="customerPhone">SĐT khách hàng.</param>
        /// <param name="totalAmount">Tổng tiền đơn (đ).</param>
        public Task SendOrderReceiptAsync(string orderNumber, string customerPhone, decimal totalAmount)
        {
            var message =
                $"Xôi Phú Thượng: đơn hàng {orderNumber} ({totalAmount:N0} ₫) đã được ghi nhận. " +
                "Cảm ơn quý khách!";
            return SendTextMessageAsync(customerPhone, message);
        }

        /// <summary>
        /// Gửi tin nhắn văn bản qua Zalo OA. Chưa cấu hình (<see cref="ZaloOaOptions.IsConfigured"/>
        /// false) thì chỉ ghi log stub; đã cấu hình nhưng chưa cài đặt gọi API
        /// thật thì ghi log cảnh báo — không ném lỗi để luồng gọi không bị gãy.
        /// </summary>
        /// <param name="phone">SĐT người nhận.</param>
        /// <param name="message">Nội dung tin nhắn.</param>
        public Task SendTextMessageAsync(string phone, string message)
        {
            if (!_options.IsConfigured)
            {
                LogZaloStub(_logger, phone, _options.OaId, message, null);
                return Task.CompletedTask;
            }

            // TODO (go-live): POST to https://openapi.zalo.me/v3.0/oa/message/cs
            // with the access token, mapping the phone to the OA user id first
            // (Zalo identifies recipients by userId, not phone).
            LogZaloNotImplemented(_logger, phone, message, null);
            return Task.CompletedTask;
        }
    }
}
