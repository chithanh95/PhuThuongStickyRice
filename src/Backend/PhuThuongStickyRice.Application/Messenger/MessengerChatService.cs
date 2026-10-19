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

namespace PhuThuongStickyRice.Application.Messenger
{
    public interface IMessengerChatService : ITransientDependency
    {
        /// <summary>
        /// Handles the Facebook webhook verification handshake (GET).
        /// Returns the challenge when the verify token matches, else null.
        /// </summary>
        Task<string> VerifyWebhookAsync(string mode, string verifyToken, string challenge);

        /// <summary>Handles an incoming Messenger webhook payload (POST).</summary>
        Task HandleWebhookAsync(JsonDocument payload);

        /// <summary>
        /// Verify chữ ký X-Hub-Signature-256 (HMAC-SHA256 bằng App Secret) của
        /// webhook POST — chống giả mạo callback tạo đơn ảo. Trả về false khi
        /// đã cấu hình App Secret mà chữ ký thiếu/sai. Khi chưa cấu hình secret
        /// thì log cảnh báo và trả về true để webhook đang chạy không bị gãy.
        /// </summary>
        Task<bool> VerifyWebhookSignatureAsync(byte[] body, string signatureHeader);
    }

    /// <summary>
    /// Simple rule-based Messenger chatbot for Xôi Phú Thượng (item 53).
    ///
    /// Conversation script:
    ///   greeting / "menu"  -> sends the menu (text) + ordering instructions
    ///   "đặt &lt;tên món&gt; x &lt;số lượng&gt;" -> order summary + asks to confirm
    ///   "ok"               -> asks for name + phone (+ shipper/self pickup)
    ///   "&lt;Tên&gt; - &lt;SĐT&gt;"       -> creates a COD order (Source = "Messenger")
    ///   "hủy"              -> cancels the current draft
    ///
    /// Setup (Facebook App dashboard):
    ///   1. Create an App, add the Messenger product, connect the Fanpage.
    ///   2. Generate a Page Access Token -> paste into
    ///      Admin &gt; Cài đặt cửa hàng &gt; Chatbot Messenger &gt; Page Access Token.
    ///   3. Pick any Verify Token string -&gt; paste it in the same place.
    ///   4. In "Webhooks", subscribe the Page to the "messages" field with
    ///      callback URL: https://&lt;domain&gt;/api/webhooks/messenger
    ///      and the verify token from step 3.
    ///
    /// Without a Page Access Token the service runs in "waiting for
    /// configuration" mode: webhook verification still works, incoming
    /// messages are logged, but no replies are sent (never crashes).
    /// Sessions are kept in memory (single-instance friendly).
    /// </summary>
    public partial class MessengerChatService : ApplicationService, IMessengerChatService
    {
        private const string GraphApiVersion = "v18.0";
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
        private const int MessengerMessageChunkSize = 1800; // Messenger caps text ~2000 chars
        private static readonly TimeSpan BetweenChunksDelay = TimeSpan.FromMilliseconds(300);

        private readonly IShopSettingAppService _shopSettings;
        private readonly IProductAppService _products;
        private readonly IOrderAppService _orders;
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly ConcurrentDictionary<string, ChatSession> _sessions =
            new ConcurrentDictionary<string, ChatSession>();

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
                actual = Convert.FromHexString(signatureHeader.Substring(prefix.Length).Trim());
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
            foreach (var l in parsed)
            {
                sb.AppendLine($"- {l.Quantity} x {l.ProductName} ({FormatVnd(l.UnitPrice * l.Quantity)})");
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
            var fulfillment = norm.Contains("shipper") ? "ShipperPickup" : "CustomerPickup";

            try
            {
                var order = await _orders.CreateAsync(
                    BuildOrderDto(session, name, phone, fulfillment),
                    skipRecaptchaCheck: true);

                _sessions.TryRemove(session.Psid ?? string.Empty, out _);
                var pickupNote = fulfillment == "ShipperPickup"
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
                name = "Khách Messenger";
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
                CustomerName = name.Length > 200 ? name.Substring(0, 200) : name,
                CustomerPhone = phone,
                CustomerAddress = fulfillment == "CustomerPickup" ? "Khách tự đến quán lấy" : "Shipper đến quán lấy",
                Notes = $"Đặt qua Messenger chat (PSID {session.Psid ?? "?"})",
                PaymentMethod = "Cod",
                FulfillmentMethod = fulfillment,
                Source = "Messenger",
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
                foreach (var p in items.Take(MenuMaxItems))
                {
                    sb.AppendLine($"- {p.Name}: {FormatVnd(p.Price)}");
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

            var body = OrderPrefixRegex().Replace(text, " ").Trim();
            var parts = body.Split(new[] { ',', ';', '+' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawPart in parts)
            {
                var part = rawPart.Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                var line = ParseOrderPart(part, items);
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
                Logger.LogWarning(ex, warnMessage);
                return new List<ProductDto>();
            }
        }

        /// <summary>
        /// Parses one order fragment like "xôi gấc x 2" into a pending
        /// line, or null when no product matches.
        /// </summary>
        private PendingLine ParseOrderPart(string part, List<ProductDto> items)
        {
            var qty = 1;
            var quantityMatch = QuantityRegex().Match(part);
            if (quantityMatch.Success)
            {
                qty = Math.Max(1, Math.Min(100, int.Parse(quantityMatch.Groups[1].Value)));
                part = (part.Substring(0, quantityMatch.Index) + " " + part.Substring(quantityMatch.Index + quantityMatch.Length)).Trim();
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

        private static ProductDto MatchProduct(string part, List<ProductDto> items)
        {
            var normPart = NormalizeForMatching(part);
            if (normPart.Length < 2)
            {
                return null;
            }

            ProductDto best = null;
            var bestScore = 0;
            foreach (var p in items)
            {
                var normName = NormalizeForMatching(p.Name);
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
                client.Timeout = TimeSpan.FromSeconds(15);
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
                    using var resp = await client.PostAsync(url, content);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var err = await resp.Content.ReadAsStringAsync();
                        Logger.LogWarning("Messenger send failed ({Status}): {Error}",
                            (int)resp.StatusCode, err);
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
            var cutoff = DateTime.UtcNow - SessionTimeout;
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
