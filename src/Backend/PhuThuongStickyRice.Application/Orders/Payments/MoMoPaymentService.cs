using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Exceptions;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Exceptions;
using PhuThuongStickyRice.Application.Orders.Payments;

namespace PhuThuongStickyRice.Application.Orders.Payments
{
    public interface IMoMoPaymentService
    {
        Task<MoMoCreatePaymentResultDto> CreatePaymentAsync(Order order);

        Task<Order> HandleCallbackAsync(MoMoCallbackDto callback);
    }

    /// <summary>
    /// MoMo payment flow. In TestMode the real MoMo endpoint is never called:
    /// the buyer is sent to the in-app simulator page, whose callback echoes
    /// the create-signature for verification. With real credentials
    /// (TestMode = false) the service posts the signed create request to MoMo
    /// and verifies the signed IPN callback the same way.
    ///
    /// Item 106: non-secret values (PartnerCode, Endpoint) are read from the
    /// database first (admin-editable at /admin/thanh-toan), falling back to
    /// appsettings. Secrets stay in appsettings only.
    /// </summary>
    public class MoMoPaymentService : IMoMoPaymentService
    {
        private const string RequestType = "captureWallet";

        private readonly IOrderRepository _orders;
        private readonly IPaymentConfigProvider _paymentConfig;
        private readonly HttpClient _httpClient;

        public MoMoPaymentService(
            IOrderRepository orders,
            IPaymentConfigProvider paymentConfig,
            HttpClient httpClient)
        {
            _orders = orders;
            _paymentConfig = paymentConfig;
            _httpClient = httpClient;
        }

        public async Task<MoMoCreatePaymentResultDto> CreatePaymentAsync(Order order)
        {
            if (order.PaymentMethod != PaymentMethod.MoMo)
            {
                throw new DomainException("Order is not a MoMo order.");
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                throw new DomainException("Order is already paid.");
            }

            var _options = await _paymentConfig.GetMoMoAsync();

            var requestId = order.Id.ToString("N");
            var amount = (long)order.TotalAmount.Amount;
            var orderId = order.OrderNumber;
            var orderInfo = $"Thanh toan don hang {order.OrderNumber} - Xoi Phu Thuong";
            var redirectUrl = $"{_options.StorefrontBaseUrl.TrimEnd('/')}/track-order?orderNumber={Uri.EscapeDataString(order.OrderNumber)}";
            var ipnUrl = $"{_options.ApiBaseUrl.TrimEnd('/')}/api/payments/momo/callback";

            var rawString = MoMoSignatureHelper.BuildCreateRawString(
                _options.PartnerCode, _options.AccessKey, requestId, amount,
                orderId, orderInfo, redirectUrl, ipnUrl, RequestType);
            var signature = MoMoSignatureHelper.Sign(rawString, _options.SecretKey);

            if (_options.TestMode)
            {
                // TEST: never call the real MoMo endpoint. The simulator page
                // receives the create-signature and echoes it back on callback.
                var payUrl = $"{_options.StorefrontBaseUrl.TrimEnd('/')}/momo-simulator"
                    + $"?orderId={order.Id}&orderNumber={Uri.EscapeDataString(order.OrderNumber)}"
                    + $"&amount={amount}&requestId={requestId}&signature={Uri.EscapeDataString(signature)}";
                return new MoMoCreatePaymentResultDto
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Amount = order.TotalAmount.Amount,
                    PayUrl = payUrl,
                    TestMode = true
                };
            }

            if (string.IsNullOrWhiteSpace(_options.Endpoint)
                || string.IsNullOrWhiteSpace(_options.PartnerCode)
                || string.IsNullOrWhiteSpace(_options.AccessKey)
                || string.IsNullOrWhiteSpace(_options.SecretKey)
                || _options.PartnerCode == "xxx"
                || _options.AccessKey == "xxx"
                || _options.SecretKey == "xxx")
            {
                throw new DomainException("MoMo is not configured. Fill in PartnerCode/AccessKey/SecretKey and Endpoint.");
            }

            var payload = new
            {
                partnerCode = _options.PartnerCode,
                accessKey = _options.AccessKey,
                requestId,
                amount = amount.ToString(),
                orderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                requestType = RequestType,
                extraData = string.Empty,
                lang = "vi",
                signature
            };

            var response = await _httpClient.PostAsync(
                _options.Endpoint,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            if (resultCode != 0)
            {
                var message = root.TryGetProperty("message", out var m) ? m.GetString() : "MoMo error";
                throw new DomainException($"MoMo create payment failed: {message}");
            }

            return new MoMoCreatePaymentResultDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Amount = order.TotalAmount.Amount,
                PayUrl = root.GetProperty("payUrl").GetString(),
                TestMode = false
            };
        }

        public async Task<Order> HandleCallbackAsync(MoMoCallbackDto callback)
        {
            var _options = await _paymentConfig.GetMoMoAsync();

            if (callback == null)
            {
                throw new DomainException("MoMo callback payload is required.");
            }

            if (callback.Simulated)
            {
                return await HandleSimulatedCallbackAsync(callback, _options);
            }

            return await HandleRealCallbackAsync(callback, _options);
        }

        private async Task<Order> HandleSimulatedCallbackAsync(MoMoCallbackDto callback, MoMoOptions options)
        {
            if (!options.TestMode)
            {
                throw new DomainException("Simulated MoMo callbacks are only accepted in TestMode.");
            }

            if (!Guid.TryParse(callback.OrderId, out var orderId))
            {
                throw new DomainException("Invalid simulated MoMo order id.");
            }

            var order = await _orders.FindAsync(orderId);
            if (order == null)
            {
                throw new OrderNotFoundException(orderId);
            }

            // Verify the echoed create-signature: recompute it from the same
            // deterministic inputs used at create time.
            var expectedSignature = await BuildCreateSignatureAsync(order);
            if (!string.Equals(expectedSignature, callback.SimulatedSignature, StringComparison.Ordinal))
            {
                throw new DomainException("Invalid simulated MoMo signature.");
            }

            // Item 128: simulated callbacks have no OrderInfo; the verified
            // echo-signature itself proves the callback belongs to this order.
            order.MarkMoMoMatched();

            return await ApplyResultAsync(order, callback.ResultCode);
        }

        private async Task<Order> HandleRealCallbackAsync(MoMoCallbackDto callback, MoMoOptions options)
        {
            // In a real IPN, MoMo's orderId is our merchant order number.
            var order = await _orders.GetByOrderNumberAsync(callback.OrderId);
            if (order == null)
            {
                throw new DomainException($"Order '{callback.OrderId}' not found.");
            }

            if (callback.Amount != (long)order.TotalAmount.Amount)
            {
                throw new DomainException("MoMo callback amount does not match the order total.");
            }

            var rawString = MoMoSignatureHelper.BuildCallbackRawString(
                options.AccessKey, callback.Amount, callback.ExtraData ?? string.Empty,
                callback.Message ?? string.Empty, callback.OrderId, callback.OrderInfo ?? string.Empty,
                "momo_wallet", options.PartnerCode, callback.PayType ?? string.Empty,
                callback.RequestId ?? string.Empty, callback.ResponseTime, callback.ResultCode,
                callback.TransId ?? string.Empty);
            var expectedSignature = MoMoSignatureHelper.Sign(rawString, options.SecretKey);
            if (!string.Equals(expectedSignature, callback.Signature, StringComparison.Ordinal))
            {
                throw new DomainException("Invalid MoMo callback signature.");
            }

            // Item 128: auto-match — parse the order number out of OrderInfo
            // and flag the order when it matches this order. Full automatic
            // reconciliation (amounts, settlement files) needs the MoMo
            // business API; see the /admin/doi-soat-momo docs.
            var parsedNumber = ParseOrderNumberFromOrderInfo(callback.OrderInfo);
            if (!string.IsNullOrEmpty(parsedNumber)
                && string.Equals(parsedNumber, order.OrderNumber, StringComparison.OrdinalIgnoreCase))
            {
                order.MarkMoMoMatched();
            }

            return await ApplyResultAsync(order, callback.ResultCode);
        }

        private async Task<Order> ApplyResultAsync(Order order, int resultCode)
        {
            // MoMo may resend callbacks — already-final states are idempotent.
            if (resultCode == 0 && order.PaymentStatus == PaymentStatus.Paid)
            {
                return order;
            }

            if (resultCode != 0 && order.PaymentStatus == PaymentStatus.Failed)
            {
                return order;
            }

            if (resultCode == 0)
            {
                order.ConfirmPayment();
            }
            else
            {
                order.MarkPaymentFailed();
            }

            await _orders.UpdateAsync(order);
            return order;
        }

        /// <summary>
        /// Item 128: extracts the merchant order number (e.g. "PX-20261014-4821")
        /// from MoMo's free-text OrderInfo. Returns null when none is found.
        /// </summary>
        private static string ParseOrderNumberFromOrderInfo(string orderInfo)
        {
            if (string.IsNullOrWhiteSpace(orderInfo))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                orderInfo, @"PX-\d{8}-\d+");
            return match.Success ? match.Value : null;
        }

        private async Task<string> BuildCreateSignatureAsync(Order order)
        {
            var options = await _paymentConfig.GetMoMoAsync();
            var requestId = order.Id.ToString("N");
            var amount = (long)order.TotalAmount.Amount;
            var orderInfo = $"Thanh toan don hang {order.OrderNumber} - Xoi Phu Thuong";
            var redirectUrl = $"{options.StorefrontBaseUrl.TrimEnd('/')}/track-order?orderNumber={Uri.EscapeDataString(order.OrderNumber)}";
            var ipnUrl = $"{options.ApiBaseUrl.TrimEnd('/')}/api/payments/momo/callback";
            var rawString = MoMoSignatureHelper.BuildCreateRawString(
                options.PartnerCode, options.AccessKey, requestId, amount,
                order.OrderNumber, orderInfo, redirectUrl, ipnUrl, RequestType);
            return MoMoSignatureHelper.Sign(rawString, options.SecretKey);
        }
    }
}
