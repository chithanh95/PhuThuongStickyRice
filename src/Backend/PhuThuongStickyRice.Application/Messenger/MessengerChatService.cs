using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Products;
using PhuThuongStickyRice.Application.Products.DTOs;
using PhuThuongStickyRice.Application.Shop;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

// codequality-team 2026-10-20 run-0245 Agent1-Naming

namespace PhuThuongStickyRice.Application.Messenger
{
    /// <summary>
    /// Hợp đồng chatbot Messenger: xác minh webhook của Facebook, xác minh
    /// chữ ký chống giả mạo, và xử lý tin nhắn khách nhắn tới Fanpage.
    /// </summary>
    public interface IMessengerChatService : ITransientDependency
    {
        /// <summary>
        /// Xử lý bước xác minh webhook của Facebook (GET): trả về challenge
        /// khi verify token khớp, ngược lại trả null.
        /// </summary>
        /// <param name="mode">Chế độ verify Facebook gửi sang ("subscribe").</param>
        /// <param name="verifyToken">Token Facebook gửi sang để đối chiếu.</param>
        /// <param name="challenge">Chuỗi challenge cần echo lại khi khớp.</param>
        /// <returns>Challenge khi xác minh thành công, null khi thất bại.</returns>
        Task<string> VerifyWebhookAsync(string mode, string verifyToken, string challenge);

        /// <summary>
        /// Xử lý payload webhook Messenger gửi tới (POST): duyệt từng tin nhắn
        /// text và phản hồi theo kịch bản; tin không phải text (postback,
        /// đính kèm) bị bỏ qua.
        /// </summary>
        /// <param name="payload">Payload JSON từ Facebook.</param>
        Task HandleWebhookAsync(JsonDocument payload);

        /// <summary>
        /// Verify chữ ký X-Hub-Signature-256 (HMAC-SHA256 bằng App Secret) của
        /// webhook POST — chống giả mạo callback tạo đơn ảo. Trả về false khi
        /// đã cấu hình App Secret mà chữ ký thiếu/sai. Khi chưa cấu hình secret
        /// thì log cảnh báo và trả về true để webhook đang chạy không bị gãy.
        /// </summary>
        /// <param name="body">Body thô của request.</param>
        /// <param name="signatureHeader">Giá trị header X-Hub-Signature-256.</param>
        /// <returns>True khi chữ ký hợp lệ (hoặc chưa cấu hình secret).</returns>
        Task<bool> VerifyWebhookSignatureAsync(byte[] body, string signatureHeader);
    }

    /// <summary>
    /// Chatbot Messenger dạng rule-based cho Xôi Phú Thượng (item 53).
    ///
    /// Kịch bản hội thoại:
    ///   chào / "menu"     → gửi thực đơn (text) + hướng dẫn đặt hàng
    ///   "đặt &lt;tên món&gt; x &lt;số lượng&gt;" → tóm tắt đơn + hỏi xác nhận
    ///   "ok"              → hỏi tên + SĐT (+ shipper/tự lấy)
    ///   "&lt;Tên&gt; - &lt;SĐT&gt;"       → tạo đơn COD (Source = "Messenger")
    ///   "hủy"             → hủy nháp đơn đang soạn
    ///
    /// Cài đặt (Facebook App dashboard):
    ///   1. Tạo App, thêm sản phẩm Messenger, kết nối Fanpage.
    ///   2. Tạo Page Access Token → dán vào Admin &gt; Cài đặt cửa hàng &gt;
    ///      Chatbot Messenger &gt; Page Access Token.
    ///   3. Chọn 1 chuỗi Verify Token bất kỳ → dán ở cùng chỗ.
    ///   4. Ở "Webhooks", subscribe Page vào field "messages" với callback URL:
    ///      https://&lt;domain&gt;/api/webhooks/messenger và verify token ở bước 3.
    ///
    /// Chưa có Page Access Token thì service chạy ở chế độ "chờ cấu hình":
    /// verify webhook vẫn làm việc, tin nhắn tới chỉ log, không trả lời (không
    /// bao giờ crash). Session hội thoại giữ trong memory (phù hợp chạy 1 instance).
    /// </summary>
    public partial class MessengerChatService : ApplicationService, IMessengerChatService
    {
        /// <summary>Phiên bản Graph API dùng để gửi tin nhắn trả lời khách.</summary>
        private const string GraphApiVersion = "v18.0";
        /// <summary>Thời gian hội thoại nháp được giữ trong memory (30 phút không nhắn thì reset).</summary>
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

        // .NET source-generated regexes (C# 13): compiled at build time, no
        // runtime Regex construction on the chat hot path.
        [GeneratedRegex(@"0(3|5|7|8|9)\d{8}")]
        private static partial Regex VnPhoneRegex();

        [GeneratedRegex(@"[-–—,.;]")]
        private static partial Regex PunctuationRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"^(đặt|dat|order)\b", RegexOptions.IgnoreCase)]
        private static partial Regex OrderPrefixRegex();

        [GeneratedRegex(@"\bx\s*(\d{1,3})\b", RegexOptions.IgnoreCase)]
        private static partial Regex QuantityRegex();

        // Sending limits & catalog fetch sizes (keep these named, not inline).
        private const int MenuMaxItems = 20;
        private const int ParseOrderMaxItems = 100;
        // [codequality-smell 2026-10-19] Magic number: độ dài tên khách tối đa
        // khi cắt ngắn tin nhắn chat trước khi tạo đơn (khớp độ dài cột DB).
        private const int MaxCustomerNameLength = 200;
        // CA1861: mảng phân tách dùng lặp đi lặp lại — để static readonly
        // thay vì new[] mỗi lần gọi (Split không mutate mảng).
        private static readonly char[] OrderPartSeparators = { ',', ';', '+' };
        private const int MessengerMessageChunkSize = 1800; // Messenger caps text ~2000 chars
        private static readonly TimeSpan BetweenChunksDelay = TimeSpan.FromMilliseconds(300);

        private const string FulfillmentShipperPickup = "ShipperPickup";
        private const string FulfillmentCustomerPickup = "CustomerPickup";
        private const string PaymentMethodCod = "Cod";
        private const string OrderSourceMessenger = "Messenger";
        private const string DefaultCustomerName = "Khách Messenger";
        private const string ShipperKeyword = "shipper";
        private const int SendTimeoutSeconds = 15;
        private const int PruneThrottleSeconds = 60;

        private readonly IShopSettingAppService _shopSettings;
        private readonly IProductAppService _products;
        private readonly IOrderAppService _orders;
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly ConcurrentDictionary<string, ChatSession> _sessions =
            new ConcurrentDictionary<string, ChatSession>();

        /// <summary>
        /// perf-hunt 2026-10-19: mốc lần dọn session gần nhất. PruneSessions()
        /// quét toàn bộ dictionary O(n) nên không chạy trên mỗi tin nhắn —
        /// throttle tối đa 1 lần/60 giây. Dọn dẹp là best-effort: session hết
        /// hạn vẫn tự bị loại khi quá 30 phút, chậm tối đa 60s không ảnh hưởng.
        /// </summary>
        private static DateTime _lastPruneUtc = DateTime.MinValue;
        private static readonly object _pruneLock = new();

        /// <summary>
        /// Khởi tạo chatbot Messenger với cài đặt shop (token/secret), catalog
        /// sản phẩm, đơn hàng và HTTP client để gửi tin nhắn qua Graph API.
        /// </summary>
        public MessengerChatService(
            IShopSettingAppService shopSettings,
            IProductAppService products,
            IOrderAppService orders,
            IHttpClientFactory httpClientFactory)
        {
            _shopSettings = shopSettings;
            _products = products;
            _orders = orders;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Xử lý bước xác minh webhook của Facebook (GET): chỉ echo challenge
        /// khi mode đúng "subscribe" và verify token khớp với cấu hình shop.
        /// </summary>
        /// <param name="mode">Chế độ verify Facebook gửi sang ("subscribe").</param>
        /// <param name="verifyToken">Token Facebook gửi sang để đối chiếu.</param>
        /// <param name="challenge">Chuỗi challenge cần echo lại khi khớp.</param>
        /// <returns>Challenge khi xác minh thành công, null khi thất bại.</returns>
        public async Task<string> VerifyWebhookAsync(string mode, string verifyToken, string challenge)
        {
            if (!string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(challenge))
            {
                return null;
            }

            var settings = await _shopSettings.GetAsync();
            var expected = settings?.FacebookVerifyToken;
            if (string.IsNullOrWhiteSpace(expected)
                || !string.Equals(verifyToken, expected, StringComparison.Ordinal))
            {
                Logger.LogWarning("Messenger webhook verification failed (token mismatch).");
                return null;
            }

            Logger.LogInformation("Messenger webhook verified successfully.");
            return challenge;
        }

        /// <summary>
        /// Verify chữ ký X-Hub-Signature-256 (HMAC-SHA256 bằng App Secret) của
        /// webhook POST — chỉ đơn tạo từ Facebook thật mới được chấp nhận.
        /// Chưa cấu hình App Secret thì log cảnh báo và cho qua (true) để
        /// webhook đang chạy theo hướng dẫn cũ không bị gãy.
        /// </summary>
        /// <param name="body">Body thô của request.</param>
        /// <param name="signatureHeader">Giá trị header X-Hub-Signature-256.</param>
        /// <returns>True khi chữ ký hợp lệ (hoặc chưa cấu hình secret).</returns>
        public async Task<bool> VerifyWebhookSignatureAsync(byte[] body, string signatureHeader)
        {
            var settings = await _shopSettings.GetAsync();
            var appSecret = settings?.FacebookAppSecret;
            if (string.IsNullOrWhiteSpace(appSecret))
            {
                // Chưa cấu hình App Secret: không thể verify — log cảnh báo
                // và cho qua để webhook đang chạy theo hướng dẫn cũ không gãy.
                // Admin nên dán App Secret (App dashboard > Settings > Basic)
                // tại Cài đặt cửa hàng > Chatbot Messenger.
                Logger.LogWarning(
                    "Messenger webhook signature NOT verified: FacebookAppSecret chưa được cấu hình.");
                return true;
            }

            const string prefix = "sha256=";
            if (string.IsNullOrWhiteSpace(signatureHeader)
                || !signatureHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Messenger webhook bị từ chối: thiếu header X-Hub-Signature-256.");
                return false;
            }

            byte[] actual;
            try
            {
                // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: range
                // operator thay Substring(prefix.Length) — guard StartsWith(prefix) ở trên.
                actual = Convert.FromHexString(signatureHeader[prefix.Length..].Trim());
            }
            catch (FormatException)
            {
                Logger.LogWarning("Messenger webhook bị từ chối: chữ ký không phải hex hợp lệ.");
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var expected = hmac.ComputeHash(body);
            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                Logger.LogWarning("Messenger webhook bị từ chối: chữ ký X-Hub-Signature-256 không hợp lệ.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Xử lý webhook POST từ Facebook: duyệt mảng "entry" → "messaging",
        /// xử lý từng tin nhắn riêng (bỏ qua event không phải text như
        /// postback/attachment). Chưa cấu hình Page Access Token thì chỉ log,
        /// không crash. Lỗi 1 tin không làm gãy các tin khác.
        /// </summary>
        /// <param name="payload">Payload JSON từ Facebook.</param>
        public async Task HandleWebhookAsync(JsonDocument payload)
        {
            string pageToken = null;
            try
            {
                var settings = await _shopSettings.GetAsync();
                pageToken = settings?.FacebookPageAccessToken;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Could not load shop settings for Messenger; continuing in log-only mode.");
            }

            if (!payload.RootElement.TryGetProperty("entry", out var entries))
            {
                return;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("messaging", out var messagings))
                {
                    continue;
                }

                foreach (var item in messagings.EnumerateArray())
                {
                    await ProcessMessageItemAsync(item, pageToken);
                }
            }
        }

        /// <summary>
        /// Processes a single Messenger webhook "messaging" item: extracts the
        /// sender + text, runs the conversation rules, and sends the reply.
        /// Non-text events (postbacks, attachments) are ignored. Extracted
        /// from HandleWebhookAsync to keep the nesting shallow.
        /// </summary>
        private async Task ProcessMessageItemAsync(JsonElement item, string pageToken)
        {
            try
            {
                var psid = item.GetProperty("sender").GetProperty("id").GetString();
                if (!item.TryGetProperty("message", out var message)
                    || !message.TryGetProperty("text", out var textProp))
                {
                    return; // ignore non-text events (postbacks, attachments...)
                }

                var text = textProp.GetString() ?? string.Empty;
                Logger.LogInformation("Messenger message from {Psid}: {Text}", psid, text);
                var reply = await ProcessMessageAsync(psid, text);
                if (reply != null)
                {
                    await SendTextAsync(psid, reply, pageToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to process a Messenger webhook item.");
            }
        }

        // ---------------- conversation ----------------

        /// <summary>
        /// Xử lý một tin nhắn trong máy trạng thái hội thoại đặt hàng qua Messenger:
        /// Browsing (xem menu/chọn món) → Confirming (xác nhận giỏ hàng) → AwaitingInfo
        /// (nhập tên/SĐT/cách nhận hàng). Gõ từ khóa hủy ở bất kỳ trạng thái nào
        /// cũng xóa session để bắt đầu lại từ menu.
        /// </summary>
        /// <param name="psid">Id người gửi trên Messenger — khóa session hội thoại.</param>
        /// <param name="text">Nội dung tin nhắn khách gửi.</param>
        /// <returns>Câu trả lời bot gửi lại cho khách.</returns>
        private async Task<string> ProcessMessageAsync(string psid, string text)
        {
            PruneSessions();
            var session = _sessions.GetOrAdd(psid, key => new ChatSession { Psid = key });
            session.LastActivity = DateTime.UtcNow;

            var norm = NormalizeForMatching(text);

            if (IsCancel(norm))
            {
                _sessions.TryRemove(psid, out _);
                return "Đã hủy. Gõ \"menu\" để xem thực đơn bất cứ lúc nào nhé! 🍚";
            }

            switch (session.State)
            {
                case ChatState.Confirming:
                    if (IsConfirm(norm))
                    {
                        session.State = ChatState.AwaitingInfo;
                        return "Tuyệt! Cho mình xin thông tin theo mẫu:\n"
                             + "Tên - SĐT - shipper lấy / tự lấy\n"
                             + "VD: Lan - 0912345678 - shipper lấy";
                    }
                    // allow re-ordering while confirming
                    return await HandleBrowsingAsync(session, text, norm);

                case ChatState.AwaitingInfo:
                    return await HandleInfoAsync(session, text);

                case ChatState.Browsing:
                default:
                    if (IsGreetingOrMenu(norm))
                    {
                        session.State = ChatState.Browsing;
                        return await BuildMenuMessageAsync();
                    }
                    return await HandleBrowsingAsync(session, text, norm);
            }
        }

        private async Task<string> HandleBrowsingAsync(ChatSession session, string text, string norm)
        {
            if (IsGreetingOrMenu(norm))
            {
                return await BuildMenuMessageAsync();
            }

            var parsed = await ParseOrderLinesAsync(text);
            if (parsed.Count == 0)
            {
                return "Mình chưa hiểu ý bạn. Gõ \"menu\" để xem thực đơn, hoặc đặt theo mẫu:\n"
                     + "đặt xôi gấc x 2, chả cốm x 1";
            }

            session.PendingLines = parsed;
            session.State = ChatState.Confirming;
            var total = parsed.Sum(x => x.UnitPrice * x.Quantity);
            var sb = new StringBuilder("Bạn đặt:\n");
            foreach (var line in parsed)
            {
                sb.AppendLine($"- {line.Quantity} x {line.ProductName} ({FormatVnd(line.UnitPrice * line.Quantity)})");
            }
            sb.AppendLine($"Tổng: {FormatVnd(total)} (COD).");
            sb.Append("Trả lời \"OK\" để xác nhận, hoặc \"hủy\" để làm lại nhé!");
            return sb.ToString();
        }

        private async Task<string> HandleInfoAsync(ChatSession session, string text)
        {
            // Tách SĐT + tên khách từ tin nhắn: SĐT là cụm số khớp regex VN,
            // phần chữ còn lại là tên (xóa dấu câu, gom khoảng trắng thừa).
            var (phone, name) = ExtractCustomerInfo(text);
            if (phone == null)
            {
                return "SĐT chưa đúng định dạng (VD: 0912345678). Bạn gửi lại giúp mình nhé!";
            }

            var norm = NormalizeForMatching(text);
            var fulfillment = norm.Contains(ShipperKeyword) ? FulfillmentShipperPickup : FulfillmentCustomerPickup;

            try
            {
                var order = await _orders.CreateAsync(
                    BuildOrderDto(session, name, phone, fulfillment),
                    skipRecaptchaCheck: true);

                _sessions.TryRemove(session.Psid ?? string.Empty, out _);
                var pickupNote = fulfillment == FulfillmentShipperPickup
                    ? "Shipper đến quán đọc mã đơn để lấy hàng nhé!"
                    : "Bạn đến quán đọc mã đơn để lấy hàng nhé!";
                return $"✅ Đặt đơn thành công!\nMã đơn: {order.OrderNumber}\n"
                     + $"Tổng tiền: {FormatVnd(order.TotalAmount)} (thanh toán khi nhận hàng).\n"
                     + pickupNote;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Messenger order creation failed.");
                return "Rất tiếc, quán đang tạm ngưng nhận đơn lúc này. Bạn thử lại sau ít phút nhé!";
            }
        }

        /// <summary>
        /// Tách SĐT + tên khách từ tin nhắn chat: SĐT là cụm số khớp regex VN,
        /// phần chữ còn lại là tên (xóa dấu câu, gom khoảng trắng thừa).
        /// Trả (null, null) khi SĐT sai định dạng.
        /// </summary>
        private (string Phone, string Name) ExtractCustomerInfo(string text)
        {
            var phoneMatch = VnPhoneRegex().Match(text.Replace(" ", string.Empty));
            if (!phoneMatch.Success)
            {
                return (null, null);
            }

            var name = VnPhoneRegex().Replace(text, " ");
            name = PunctuationRegex().Replace(name, " ").Trim();
            name = WhitespaceRegex().Replace(name, " ");
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                name = DefaultCustomerName;
            }

            return (phoneMatch.Value, name);
        }

        /// <summary>
        /// Dựng DTO tạo đơn từ thông tin khách chat: thanh toán COD, nguồn
        /// Messenger, địa chỉ theo hình thức nhận. reCAPTCHA bỏ qua ở call site
        /// vì chat không có token trình duyệt.
        /// </summary>
        private static CreateOrderDto BuildOrderDto(
            ChatSession session, string name, string phone, string fulfillment)
        {
            return new CreateOrderDto
            {
                // codequality-team 2026-10-20 run-0245 Agent3-BestPractices: range
                // operator thay Substring(0, N) — guard Length ở điều kiện nên tương đương.
                CustomerName = name.Length > MaxCustomerNameLength ? name[..MaxCustomerNameLength] : name,
                CustomerPhone = phone,
                CustomerAddress = fulfillment == FulfillmentCustomerPickup ? "Khách tự đến quán lấy" : "Shipper đến quán lấy",
                Notes = $"Đặt qua Messenger chat (PSID {session.Psid ?? "?"})",
                PaymentMethod = PaymentMethodCod,
                FulfillmentMethod = fulfillment,
                Source = OrderSourceMessenger,
                Lines = session.PendingLines.Select(x => new CreateOrderLineDto
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity
                }).ToList()
            };
        }

        // ---------------- menu & parsing ----------------

        private async Task<string> BuildMenuMessageAsync()
        {
            var items = await LoadActiveProductsAsync(
                MenuMaxItems, "Could not load product menu for Messenger.");

            var sb = new StringBuilder("🍚 THỰC ĐƠN XÔI PHÚ THƯỢNG\n");
            if (items.Count == 0)
            {
                sb.Append("(Quán đang cập nhật thực đơn, bạn gọi hotline giúp mình nhé!)");
            }
            else
            {
                // LoadActiveProductsAsync đã giới hạn server-side đúng
                // MenuMaxItems (MaxResultCount) nên không Take lại ở đây.
                foreach (var product in items)
                {
                    sb.AppendLine($"- {product.Name}: {FormatVnd(product.Price)}");
                }
            }
            sb.Append("\nĐặt theo mẫu: đặt xôi gấc x 2, chả cốm x 1");
            return sb.ToString();
        }

        private async Task<List<PendingLine>> ParseOrderLinesAsync(string text)
        {
            var result = new List<PendingLine>();
            var items = await LoadActiveProductsAsync(
                ParseOrderMaxItems, "Could not load products for Messenger order parsing.");

            // perf-hunt 2026-10-19: chuẩn hóa tên món 1 lần cho cả tin nhắn
            // thay vì NormalizeForMatching(p.Name) lại cho MỖI món × MỖI đoạn
            // (Unicode decompose + regex mỗi lần gọi). Kết quả so khớp giống
            // hệt vì cùng hàm chuẩn hóa, chỉ tính trước.
            var normalizedItems = items
                .Select(p => (Product: p, NormName: NormalizeForMatching(p.Name)))
                .ToList();

            var body = OrderPrefixRegex().Replace(text, " ").Trim();
            var parts = body.Split(OrderPartSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawPart in parts)
            {
                var part = rawPart.Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                var line = ParseOrderPart(part, normalizedItems);
                if (line != null)
                {
                    result.Add(line);
                }
            }

            return result;
        }

        /// <summary>
        /// Loads the active product catalog for Messenger features.
        /// Never throws — returns an empty list when the catalog is unavailable.
        /// </summary>
        private async Task<List<ProductDto>> LoadActiveProductsAsync(int maxItems, string warnMessage)
        {
            try
            {
                var paged = await _products.GetListAsync(new GetProductsInput
                {
                    Status = "Active",
                    MaxResultCount = maxItems
                });
                return paged?.Items?.ToList() ?? new List<ProductDto>();
            }
            catch (Exception ex)
            {
                // CA2254: không truyền chuỗi động làm message template (chuỗi
                // chứa dấu { } sẽ bị hiểu nhầm thành placeholder) — truyền
                // qua argument {WarnMessage} thì nội dung log ra y hệt.
                Logger.LogWarning(ex, "{WarnMessage}", warnMessage);
                return new List<ProductDto>();
            }
        }

        /// <summary>
        /// Parses one order fragment like "xôi gấc x 2" into a pending
        /// line, or null when no product matches.
        /// </summary>
        private PendingLine ParseOrderPart(
            string part, List<(ProductDto Product, string NormName)> items)
        {
            var qty = 1;
            var quantityMatch = QuantityRegex().Match(part);
            if (quantityMatch.Success)
            {
                qty = Math.Max(1, Math.Min(100, int.Parse(quantityMatch.Groups[1].Value, CultureInfo.InvariantCulture)));
                // CA1845: dùng span-based Concat thay cho Substring — không cấp phát chuỗi trung gian.
                part = string.Concat(
                    part.AsSpan(0, quantityMatch.Index),
                    " ".AsSpan(),
                    part.AsSpan(quantityMatch.Index + quantityMatch.Length)).Trim();
            }

            var product = MatchProduct(part, items);
            return product == null
                ? null
                : new PendingLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = qty
                };
        }

        private static ProductDto MatchProduct(
            string part, List<(ProductDto Product, string NormName)> items)
        {
            var normPart = NormalizeForMatching(part);
            if (normPart.Length < 2)
            {
                return null;
            }

            ProductDto best = null;
            var bestScore = 0;
            foreach (var (p, normName) in items)
            {
                var score = 0;
                // every word of the query found in the product name scores
                foreach (var w in normPart.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (w.Length >= 2 && normName.Contains(w))
                    {
                        score += w.Length;
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            return bestScore >= 3 ? best : null;
        }

        // ---------------- sending ----------------

        private async Task SendTextAsync(string psid, string text, string pageToken)
        {
            if (string.IsNullOrWhiteSpace(pageToken))
            {
                // Waiting-for-configuration mode: log instead of sending, never crash.
                Logger.LogInformation("[Messenger: chưa cấu hình Page token] -> {Psid}: {Text}", psid, text);
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(SendTimeoutSeconds);
                // Messenger caps a single text message around 2000 chars.
                foreach (var chunk in SplitMessage(text, MessengerMessageChunkSize))
                {
                    var url = $"https://graph.facebook.com/{GraphApiVersion}/me/messages"
                              + $"?access_token={Uri.EscapeDataString(pageToken)}";
                    var body = new
                    {
                        recipient = new { id = psid },
                        messaging_type = "RESPONSE",
                        message = new { text = chunk }
                    };
                    var json = JsonSerializer.Serialize(body);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var response = await client.PostAsync(url, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        Logger.LogWarning("Messenger send failed ({Status}): {Error}",
                            (int)response.StatusCode, err);
                        break;
                    }
                    await Task.Delay(BetweenChunksDelay);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Exception while sending Messenger message.");
            }
        }

        // ---------------- helpers ----------------

        private static void PruneSessions()
        {
            // Throttle: mỗi 60 giây mới quét 1 lần — gọi trên mỗi tin nhắn
            // (ProcessMessageAsync) với O(n) scan sẽ tốn CPU vô ích khi bot
            // đông. Lock nhẹ là đủ vì đây chỉ là heuristic best-effort.
            var now = DateTime.UtcNow;
            lock (_pruneLock)
            {
                if (now - _lastPruneUtc < TimeSpan.FromSeconds(PruneThrottleSeconds))
                {
                    return;
                }
                _lastPruneUtc = now;
            }

            var cutoff = now - SessionTimeout;
            foreach (var kv in _sessions)
            {
                if (kv.Value.LastActivity < cutoff)
                {
                    _sessions.TryRemove(kv.Key, out _);
                }
            }
        }

        private static bool IsGreetingOrMenu(string norm)
        {
            return norm.Contains("menu") || norm.Contains("thuc don") || norm.Contains("thực đơn")
                || norm is "hi" or "hello" or "chao" or "chào" || norm.StartsWith("chao ", StringComparison.Ordinal)
                || norm.Contains("gia") && norm.Contains("ban");
        }

        private static bool IsConfirm(string norm)
        {
            return norm is "ok" or "okay" or "yes" || norm.Contains("dong y") || norm.Contains("đồng ý")
                || norm.Contains("chot") || norm.Contains("chốt") || norm.Contains("xac nhan")
                || norm.Contains("xác nhận");
        }

        private static bool IsCancel(string norm)
        {
            return norm is "huy" or "huỷ" or "cancel" || norm.Contains("huy don") || norm.Contains("huỷ đơn");
        }

        /// <summary>
        /// Chuẩn hoá văn bản tin nhắn để so khớp từ khoá: bỏ dấu tiếng Việt, gộp
        /// khoảng trắng, chữ thường. Dùng khi bot Messenger nhận diện ý định
        /// (huỷ đơn, hỏi giá...) bất kể khách gõ có dấu hay không.
        /// </summary>
        private static string NormalizeForMatching(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return WhitespaceRegex().Replace(sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), " ").Trim();
        }

        private static string FormatVnd(decimal amount)
        {
            return $"{amount:N0}đ";
        }

        private static IEnumerable<string> SplitMessage(string text, int maxLen)
        {
            for (var i = 0; i < text.Length; i += maxLen)
            {
                yield return text.Substring(i, Math.Min(maxLen, text.Length - i));
            }
        }

        private sealed class ChatSession
        {
            public string Psid { get; set; }
            public ChatState State { get; set; } = ChatState.Browsing;
            public List<PendingLine> PendingLines { get; set; } = new List<PendingLine>();
            public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        }

        private enum ChatState
        {
            Browsing,
            Confirming,
            AwaitingInfo
        }

        private sealed class PendingLine
        {
            public Guid ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
        }
    }
}
