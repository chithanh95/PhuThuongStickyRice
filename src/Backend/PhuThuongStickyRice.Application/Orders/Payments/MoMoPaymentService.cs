using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Exceptions;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.Repositories;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Application.Orders.Payments
{
    /// <summary>
    /// Hợp đồng thanh toán MoMo: tạo yêu cầu thanh toán (trả URL để khách
    /// chuyển sang trang MoMo thanh toán) và xử lý callback MoMo gửi về
    /// để ghi nhận trạng thái đã trả/tiền bị từ chối.
    /// </summary>
    public interface IMoMoPaymentService
    {
        /// <summary>
        /// Tạo yêu cầu thanh toán MoMo cho đơn hàng và trả về URL để khách
        /// tiếp tục thanh toán. Ở TestMode thì trả URL trang giả lập trong
        /// app thay vì gọi API thật của MoMo.
        /// </summary>
        /// <param name="order">Đơn hàng cần thanh toán (phải dùng phương thức MoMo và chưa trả tiền).</param>
        /// <returns>Thông tin thanh toán gồm URL chuyển hướng.</returns>
        Task<MoMoCreatePaymentResultDto> CreatePaymentAsync(Order order);

        /// <summary>
        /// Xử lý callback MoMo gửi về (IPN thật) hoặc callback giả lập (TestMode):
        /// kiểm tra chữ ký, kiểm tra số tiền khớp với đơn, rồi cập nhật trạng
        /// thái thanh toán của đơn (đã trả / thất bại).
        /// </summary>
        /// <param name="callback">Dữ liệu callback MoMo gửi về.</param>
        /// <returns>Đơn hàng sau khi cập nhật trạng thái thanh toán.</returns>
        Task<Order> HandleCallbackAsync(MoMoCallbackDto callback);
    }

    /// <summary>
    /// Luồng thanh toán MoMo. Ở TestMode không bao giờ gọi endpoint thật của
    /// MoMo: khách được chuyển tới trang giả lập trong app, và callback của
    /// trang giả lập sẽ echo lại chữ ký tạo đơn để service đối chiếu. Với
    /// thông tin thật (TestMode = false), service gửi yêu cầu tạo đơn đã ký
    /// HMAC tới MoMo và xác minh callback IPN có chữ ký theo cùng cách.
    /// Item 106: các giá trị không phải secret (PartnerCode, Endpoint) đọc từ
    /// DB trước (admin sửa ở /admin/thanh-toan), thiếu thì mới lấy từ
    /// appsettings. Secret chỉ lưu ở appsettings.
    /// </summary>
    public partial class MoMoPaymentService : IMoMoPaymentService
    {
        private const string RequestType = "captureWallet";
        /// <summary>Giá trị placeholder trong cấu hình — gặp giá trị này ở PartnerCode/AccessKey/SecretKey thì coi như MoMo chưa được cấu hình.</summary>
        private const string UnconfiguredPlaceholder = "xxx";
        /// <summary>Ngôn ngữ hiển thị trên trang thanh toán MoMo (tiếng Việt).</summary>
        private const string MoMoUiLanguage = "vi";
        /// <summary>Đường dẫn trang tra cứu đơn trên storefront — MoMo redirect khách về đây sau khi thanh toán xong.</summary>
        private const string TrackOrderPath = "/track-order";
        /// <summary>Đường dẫn trang giả lập MoMo trong app — dùng khi TestMode = true để không gọi API thật của MoMo.</summary>
        private const string MoMoSimulatorPath = "/momo-simulator";
        /// <summary>Đường dẫn webhook nhận kết quả thanh toán MoMo gửi về (IPN callback).</summary>
        private const string MoMoCallbackPath = "/api/payments/momo/callback";

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

        /// <summary>
        /// Tạo yêu cầu thanh toán MoMo cho đơn hàng: ký HMAC rồi trả URL cho
        /// khách đi thanh toán. TestMode → trang giả lập trong app; không phải
        /// TestMode → gọi API thật của MoMo.
        /// </summary>
        /// <param name="order">Đơn hàng cần thanh toán (phải dùng MoMo, chưa trả tiền).</param>
        /// <returns>URL chuyển hướng thanh toán và cờ TestMode.</returns>
        public async Task<MoMoCreatePaymentResultDto> CreatePaymentAsync(Order order)
        {
            ValidateMoMoOrder(order);

            var _options = await _paymentConfig.GetMoMoAsync();
            var request = BuildSignedCreateRequest(order, _options);

            if (_options.TestMode)
            {
                // TEST: never call the real MoMo endpoint. The simulator page
                // receives the create-signature and echoes it back on callback.
                return BuildTestModeResult(order, _options, request);
            }

            ValidateMoMoConfiguration(_options);
            var payUrl = await SendCreatePaymentRequestAsync(_options, request);

            return new MoMoCreatePaymentResultDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Amount = order.TotalAmount.Amount,
                PayUrl = payUrl,
                TestMode = false
            };
        }

        /// <summary>Kiểm tra đơn hàng đủ điều kiện tạo thanh toán MoMo (đúng phương thức, chưa thanh toán).</summary>
        private static void ValidateMoMoOrder(Order order)
        {
            if (order.PaymentMethod != PaymentMethod.MoMo)
            {
                throw new DomainException("Order is not a MoMo order.");
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                throw new DomainException("Order is already paid.");
            }
        }

        /// <summary>Các tham số đã ký HMAC của một yêu cầu tạo thanh toán MoMo.</summary>
        private sealed class SignedCreateRequest
        {
            public string RequestId { get; init; } = string.Empty;
            public long Amount { get; init; }
            public string OrderId { get; init; } = string.Empty;
            public string OrderInfo { get; init; } = string.Empty;
            public string RedirectUrl { get; init; } = string.Empty;
            public string IpnUrl { get; init; } = string.Empty;
            public string Signature { get; init; } = string.Empty;
        }

        /// <summary>
        /// Dựng các URL (redirect sau thanh toán, IPN callback) và chữ ký HMAC cho
        /// yêu cầu tạo thanh toán. RequestId dùng Guid dạng "N" của đơn hàng để
        /// phía callback đối chiếu được với đơn đã tạo.
        /// </summary>
        private static SignedCreateRequest BuildSignedCreateRequest(Order order, MoMoOptions options)
        {
            var requestId = order.Id.ToString("N");
            var amount = (long)order.TotalAmount.Amount;
            var orderInfo = $"Thanh toan don hang {order.OrderNumber} - Xoi Phu Thuong";
            var redirectUrl = $"{options.StorefrontBaseUrl.TrimEnd('/')}{TrackOrderPath}?orderNumber={Uri.EscapeDataString(order.OrderNumber)}";
            var ipnUrl = $"{options.ApiBaseUrl.TrimEnd('/')}{MoMoCallbackPath}";

            var rawString = MoMoSigner.BuildCreateRawString(
                options.PartnerCode, options.AccessKey, requestId, amount,
                order.OrderNumber, orderInfo, redirectUrl, ipnUrl, RequestType);
            var signature = MoMoSigner.Sign(rawString, options.SecretKey);

            return new SignedCreateRequest
            {
                RequestId = requestId,
                Amount = amount,
                OrderId = order.OrderNumber,
                OrderInfo = orderInfo,
                RedirectUrl = redirectUrl,
                IpnUrl = ipnUrl,
                Signature = signature
            };
        }

        /// <summary>
        /// Kết quả ở TestMode: không gọi MoMo thật, trả URL trang giả lập trong app
        /// kèm chữ ký để trang giả lập echo lại khi callback.
        /// </summary>
        private static MoMoCreatePaymentResultDto BuildTestModeResult(
            Order order, MoMoOptions options, SignedCreateRequest request)
        {
            var payUrl = $"{options.StorefrontBaseUrl.TrimEnd('/')}{MoMoSimulatorPath}"
                + $"?orderId={order.Id}&orderNumber={Uri.EscapeDataString(order.OrderNumber)}"
                + $"&amount={request.Amount}&requestId={request.RequestId}&signature={Uri.EscapeDataString(request.Signature)}";
            return new MoMoCreatePaymentResultDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Amount = order.TotalAmount.Amount,
                PayUrl = payUrl,
                TestMode = true
            };
        }

        /// <summary>Kiểm tra cấu hình MoMo thật đã đủ dùng (không trống, không còn giá trị placeholder).</summary>
        private static void ValidateMoMoConfiguration(MoMoOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Endpoint)
                || string.IsNullOrWhiteSpace(options.PartnerCode)
                || string.IsNullOrWhiteSpace(options.AccessKey)
                || string.IsNullOrWhiteSpace(options.SecretKey)
                || options.PartnerCode == UnconfiguredPlaceholder
                || options.AccessKey == UnconfiguredPlaceholder
                || options.SecretKey == UnconfiguredPlaceholder)
            {
                throw new DomainException("MoMo is not configured. Fill in PartnerCode/AccessKey/SecretKey and Endpoint.");
            }
        }

        /// <summary>Gửi yêu cầu tạo thanh toán đã ký tới MoMo, trả về payUrl khi resultCode = 0.</summary>
        private async Task<string> SendCreatePaymentRequestAsync(MoMoOptions options, SignedCreateRequest request)
        {
            var payload = new
            {
                partnerCode = options.PartnerCode,
                accessKey = options.AccessKey,
                requestId = request.RequestId,
                amount = request.Amount.ToString(),
                orderId = request.OrderId,
                orderInfo = request.OrderInfo,
                redirectUrl = request.RedirectUrl,
                ipnUrl = request.IpnUrl,
                requestType = RequestType,
                extraData = string.Empty,
                lang = MoMoUiLanguage,
                signature = request.Signature
            };

            // [codequality-practices 2026-10-19] HttpResponseMessage phải dispose
            // để trả connection về pool ngay sau khi đọc xong body (trước đó
            // response không dispose, giữ socket tới khi GC — cùng lỗi đã fix
            // ở CustomerCareService/CartSyncService).
            using var response = await _httpClient.PostAsync(
                options.Endpoint,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            // Parse thẳng từ stream (async) thay vì buffer toàn bộ body thành
            // string rồi parse lại — ít allocation, không chặn thread I/O.
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            var resultCode = root.TryGetProperty("resultCode", out var resultCodeElement) ? resultCodeElement.GetInt32() : -1;
            if (resultCode != 0)
            {
                var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "MoMo error";
                throw new DomainException($"MoMo create payment failed: {message}");
            }

            return root.GetProperty("payUrl").GetString();
        }

        /// <summary>
        /// Xử lý callback MoMo: callback giả lập (chỉ chấp nhận ở TestMode) hoặc
        /// callback IPN thật. Cả hai đều phải qua kiểm tra chữ ký — callback không
        /// ký đúng không bao giờ được đổi trạng thái đơn.
        /// </summary>
        /// <param name="callback">Dữ liệu callback MoMo gửi về.</param>
        /// <returns>Đơn hàng sau khi cập nhật trạng thái thanh toán.</returns>
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

        /// <summary>
        /// Xử lý callback giả lập ở TestMode: không có chữ ký MoMo thật nên
        /// chỉ chấp nhận khi TestMode đang bật, và chữ ký echo từ trang giả
        /// lập phải đúng chữ ký tạo đơn — ngăn việc ai đó dựng 1 callback giả
        /// tự ý đổi trạng thái thanh toán.
        /// </summary>
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
            // perf-hunt 2026-10-20: truyền options đã nạp sẵn thay vì đọc
            // lại GetMoMoAsync() lần 2 trong BuildCreateSignatureAsync.
            var expectedSignature = BuildCreateSignature(order, options);
            if (!SignaturesEqual(expectedSignature, callback.SimulatedSignature))
            {
                throw new DomainException("Invalid simulated MoMo signature.");
            }

            // Item 128: simulated callbacks have no OrderInfo; the verified
            // echo-signature itself proves the callback belongs to this order.
            order.MarkMoMoMatched();

            return await ApplyResultAsync(order, callback.ResultCode);
        }

        /// <summary>
        /// Xử lý callback IPN thật của MoMo: tìm đơn theo số đơn, kiểm tra số
        /// tiền gửi về phải đúng tổng đơn (chống việc giả callback với số
        /// tiền khác), xác minh chữ ký HMAC, rồi cập nhật trạng thái.
        /// </summary>
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

            var rawString = MoMoSigner.BuildCallbackRawString(
                options.AccessKey, callback.Amount, callback.ExtraData ?? string.Empty,
                callback.Message ?? string.Empty, callback.OrderId, callback.OrderInfo ?? string.Empty,
                "momo_wallet", options.PartnerCode, callback.PayType ?? string.Empty,
                callback.RequestId ?? string.Empty, callback.ResponseTime, callback.ResultCode,
                callback.TransId ?? string.Empty);
            var expectedSignature = MoMoSigner.Sign(rawString, options.SecretKey);
            if (!SignaturesEqual(expectedSignature, callback.Signature))
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

        /// <summary>
        /// Áp kết quả thanh toán vào đơn. Idempotent: MoMo có thể gửi lại
        /// callback nhiều lần (mạng retry) — đơn đã ở trạng thái cuối (Paid /
        /// Failed) thì trả về nguyên trạng, không ghi đè.
        /// </summary>
        private async Task<Order> ApplyResultAsync(Order order, int resultCode)
        {
            // MoMo có thể gửi lại callback khi timeout/mất mạng — đơn đã chốt
            // trạng thái cuối thì bỏ qua để không flip qua lại.
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

            // GeneratedRegex: regex được biên dịch ngay lúc build (không tốn
            // Regex.Compile ở runtime, không cache động như Regex.Match static) —
            // cùng convention với MessengerChatService/OrderIntentParser.
            var match = OrderNumberRegex().Match(orderInfo);
            return match.Success ? match.Value : null;
        }

        [GeneratedRegex(@"PX-\d{8}-\d+")]
        private static partial Regex OrderNumberRegex();

        /// <summary>
        /// So sánh chữ ký HMAC chống timing attack: CryptographicOperations.
        /// FixedTimeEquals so sánh byte-by-byte trong thời gian cố định, còn
        /// string.Equals(Ordinal) thoát sớm ở ký tự khác nhau đầu tiên — kẻ
        /// tấn công có thể đo thời gian phản hồi để đoán dần từng ký tự của
        /// chữ ký đúng (đặc biệt nguy hiểm với callback IPN MoMo tiền thật).
        /// Null ở bất kỳ phía nào → false (không ném).
        /// </summary>
        private static bool SignaturesEqual(string expected, string actual)
        {
            if (expected == null || actual == null)
            {
                return false;
            }

            // Cả hai đều là chuỗi hex chữ thường do MoMoSigner.Sign sinh —
            // encode UTF-8 1 byte/ký tự nên so byte tương đương so ký tự.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual));
        }

        /// <summary>
        /// Dựng lại chữ ký tạo đơn để verify callback giả lập: thuần tính
        /// toán đồng bộ (không còn đọc cấu hình — caller truyền options đã
        /// nạp sẵn, tránh 1 lần GetMoMoAsync() thừa mỗi callback).
        /// </summary>
        private static string BuildCreateSignature(Order order, MoMoOptions options)
        {
            var requestId = order.Id.ToString("N");
            var amount = (long)order.TotalAmount.Amount;
            var orderInfo = $"Thanh toan don hang {order.OrderNumber} - Xoi Phu Thuong";
            var redirectUrl = $"{options.StorefrontBaseUrl.TrimEnd('/')}/track-order?orderNumber={Uri.EscapeDataString(order.OrderNumber)}";
            var ipnUrl = $"{options.ApiBaseUrl.TrimEnd('/')}/api/payments/momo/callback";
            var rawString = MoMoSigner.BuildCreateRawString(
                options.PartnerCode, options.AccessKey, requestId, amount,
                order.OrderNumber, orderInfo, redirectUrl, ipnUrl, RequestType);
            return MoMoSigner.Sign(rawString, options.SecretKey);
        }
    }
}
