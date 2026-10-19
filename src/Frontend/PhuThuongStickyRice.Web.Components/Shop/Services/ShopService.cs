using PhuThuongStickyRice.Web.Components.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Calls the public (anonymous) WebAPI endpoints used by the storefront.
    /// </summary>
    public class ShopService
    {
        private readonly HttpClient _httpClient;

        public ShopService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>Lấy trang đầu danh sách sản phẩm đang bán (không lọc chi nhánh).</summary>
        public async Task<PagedResult<ShopProductModel>> GetProductsAsync()
        {
            return await GetProductsAsync(null);
        }

        /// <summary>Item 109: optional branch filter (products of the branch + global products).</summary>
        public async Task<PagedResult<ShopProductModel>> GetProductsAsync(Guid? branchId)
        {
            // Only show Active products; Inactive (sold-out) are hidden.
            var url = "api/products?page=1&pageSize=100&status=Active&includeInactive=false";
            if (branchId.HasValue)
            {
                url += $"&branch={branchId.Value}";
            }

            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<PagedResult<ShopProductModel>>();
        }

        /// <summary>Validates a single product id (used when importing a shared cart).</summary>
        public async Task<ShopProductModel> GetProductAsync(Guid id)
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync($"api/products/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<ShopProductModel>();
        }

        /// <summary>Active toppings for the quick-view checkboxes.</summary>
        public async Task<List<ToppingModel>> GetToppingsAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/toppings/active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ToppingModel>>();
        }

        /// <summary>Active family combos for the shop.</summary>
        public async Task<List<ComboModel>> GetCombosAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/combos/active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ComboModel>>();
        }

        /// <summary>Product ids ordered by quantity sold (for "Bán chạy" sort).</summary>
        public async Task<List<Guid>> GetBestSellerIdsAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/products/best-sellers?top=100");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<Guid>>();
        }

        /// <summary>
        /// BA shop item 4: total quantity sold per product, for the
        /// "Đã bán N suất" social proof on product cards. Fail-soft.
        /// </summary>
        public async Task<Dictionary<Guid, long>> GetSoldQuantitiesAsync()
        {
            try
            {
                // Dispose response để trả kết nối HTTP về pool (CA2000).
                using var response = await _httpClient.GetAsync("api/products/sold-quantities");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAs<Dictionary<Guid, long>>()
                    ?? new Dictionary<Guid, long>();
            }
            catch
            {
                return new Dictionary<Guid, long>();
            }
        }

        /// <summary>
        /// Wave 6b (item 26): per-product rating aggregates
        /// {ProductId, AverageRating, ReviewCount}. Fail-soft: the shop
        /// page hides the stars when this call fails.
        /// </summary>
        public async Task<List<ProductRatingModel>> GetProductRatingsAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/products/ratings");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ProductRatingModel>>()
                ?? new List<ProductRatingModel>();
        }

        /// <summary>Quantity of items sold today ("Đã bán N gói hôm nay").</summary>
        public async Task<long> GetTodaySoldAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/dashboard/today-sold");
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadAs<TodaySoldModel>();
            return model?.QuantitySold ?? 0;
        }

        /// <summary>
        /// Item 165: daily-limit status of limited products ("Còn N suất").
        /// Fail-soft — the shop hides the badge when the call fails.
        /// </summary>
        public async Task<List<ProductDailyRemainingModel>> GetDailyRemainingAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/products/daily-remaining");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ProductDailyRemainingModel>>()
                ?? new List<ProductDailyRemainingModel>();
        }

        /// <summary>Submits a bulk/event quote request (đặt tiệc). No payment.</summary>
        public async Task CreateQuoteAsync(QuoteRequestModel quote)
        {
            // HttpRequestMessage và HttpResponseMessage đều giữ tài nguyên —
            // dispose cả hai sau khi dùng xong (CA2000).
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/quotes");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = quote.AsJsonContent();

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Subscribes a phone to monthly cung reminders (public).</summary>
        public async Task<CungReminderModel> CreateCungReminderAsync(CungReminderModel model)
        {
            // HttpRequestMessage và HttpResponseMessage đều giữ tài nguyên —
            // dispose cả hai sau khi dùng xong (CA2000).
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/quotes/cung-reminders");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = model.AsJsonContent();

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<CungReminderModel>();
        }

        /// <summary>
        /// Last order name/address for a phone number (for checkout auto-fill).
        /// Returns null when no previous order with an address exists.
        /// </summary>
        public async Task<LastOrderInfoModel> GetLastOrderInfoAsync(string phone)
        {
            // Dispose response để trả kết nối HTTP về pool — using var vẫn
            // dispose đúng cả khi return sớm ở nhánh 204 NoContent (CA2000).
            using var response = await _httpClient.GetAsync(
                $"api/orders/last-by-phone?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<LastOrderInfoModel>();
        }

        /// <summary>
        /// Item 143: first-order voucher for a new customer. Returns null
        /// when the call fails or the phone is invalid (fail-soft).
        /// </summary>
        public async Task<FirstOrderVoucherModel> GetFirstOrderVoucherAsync(string phone)
        {
            try
            {
                // Dispose response để trả kết nối HTTP về pool (CA2000).
                using var response = await _httpClient.GetAsync(
                    $"api/orders/first-order-voucher?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadAs<FirstOrderVoucherModel>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Tạo đơn hàng mới (checkout) và trả về đơn đã đặt.</summary>
        public async Task<PlacedOrderModel> CreateOrderAsync(CreateOrderModel order)
        {
            // HttpRequestMessage và HttpResponseMessage đều giữ tài nguyên —
            // dispose cả hai sau khi dùng xong (CA2000).
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/orders");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = order.AsJsonContent();

            using var response = await _httpClient.SendAsync(request);
            // SH5: đọc message lỗi tiếng Việt từ server trước khi ném.
            // EnsureSuccessStatusCode() cũ chỉ ném "Response status code does not
            // indicate success: 400 (Bad Request)." nên Checkout không bao giờ
            // hiện được lý do thật (mã hết hạn, hết hàng, SĐT bị chặn, ví thiếu...).
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(await ReadServerErrorMessageAsync(response));
            }
            return await response.Content.ReadAs<PlacedOrderModel>();
        }

        /// <summary>
        /// Trích message lỗi từ body response: ưu tiên <c>error.message</c>
        /// theo format lỗi chuẩn của ABP, fallback là body thô (cắt 500 ký tự).
        /// </summary>
        private static async Task<string> ReadServerErrorMessageAsync(HttpResponseMessage response)
        {
            string body = null;
            try
            {
                body = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                // Không đọc được body thì dùng message chung bên dưới.
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("error", out var error)
                        && error.TryGetProperty("message", out var message)
                        && message.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(message.GetString()))
                    {
                        return message.GetString();
                    }
                }
                catch
                {
                    // Body không phải JSON thì trả nguyên văn bên dưới.
                }

                return body.Length > 500 ? body.Substring(0, 500).Trim() : body.Trim();
            }

            return $"Đặt hàng thất bại (mã lỗi {(int)response.StatusCode}).";
        }

        /// <summary>Public bank-transfer details (account info + VietQR config).</summary>
        public async Task<BankTransferInfoModel> GetBankTransferInfoAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/payments/bank-transfer-info");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<BankTransferInfoModel>();
        }

        /// <summary>Customer reports they sent the bank transfer (phone must match the order).</summary>
        public async Task<TrackedOrderModel> MarkTransferredAsync(Guid orderId, string phone)
        {
            // HttpRequestMessage và HttpResponseMessage đều giữ tài nguyên —
            // dispose cả hai sau khi dùng xong (CA2000).
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/orders/{orderId}/mark-transferred");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new { CustomerPhone = phone }.AsJsonContent();

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<TrackedOrderModel>();
        }

        /// <summary>Creates a MoMo payment (test simulator payUrl while TestMode is on).</summary>
        public async Task<MoMoCreatePaymentResultModel> CreateMoMoPaymentAsync(Guid orderId)
        {
            // HttpRequestMessage và HttpResponseMessage đều giữ tài nguyên —
            // dispose cả hai sau khi dùng xong (CA2000).
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/orders/{orderId}/momo/create");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<MoMoCreatePaymentResultModel>();
        }

        /// <summary>Posts a MoMo return/callback (used by the TEST simulator page).</summary>
        public async Task MoMoCallbackAsync(MoMoCallbackModel callback)
        {
            // HttpRequestMessage và HttpResponseMessage đều giữ tài nguyên —
            // dispose cả hai sau khi dùng xong (CA2000).
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/momo/callback");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = callback.AsJsonContent();

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Public order lookup by order number + phone.</summary>
        public async Task<TrackedOrderModel> TrackOrderAsync(string orderNumber, string phone)
        {
            var url = $"api/orders/track?orderNumber={Uri.EscapeDataString(orderNumber ?? string.Empty)}"
                + $"&phone={Uri.EscapeDataString(phone ?? string.Empty)}";
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<TrackedOrderModel>();
        }

        /// <summary>Public: full order history for a VN phone number (no login).</summary>
        public async Task<List<TrackedOrderModel>> GetOrdersByPhoneAsync(string phone)
        {
            var url = $"api/orders/by-phone?phone={Uri.EscapeDataString(phone ?? string.Empty)}";
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<TrackedOrderModel>>();
        }
    }
}
