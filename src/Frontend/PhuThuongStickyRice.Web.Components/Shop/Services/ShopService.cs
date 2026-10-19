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
    /// Gọi các endpoint WebAPI public (không cần đăng nhập) phục vụ
    /// cửa hàng: sản phẩm, topping, combo, đánh giá, đơn hàng, thanh toán.
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

        /// <summary>Item 109: lọc theo chi nhánh (món của chi nhánh + món toàn hệ thống).</summary>
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

        /// <summary>Kiểm tra 1 id sản phẩm còn hợp lệ không (dùng khi nhập giỏ hàng được chia sẻ).</summary>
        public async Task<ShopProductModel> GetProductAsync(Guid id)
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync($"api/products/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<ShopProductModel>();
        }

        /// <summary>Danh sách topping đang bán (cho checkbox trong popup xem nhanh).</summary>
        public async Task<List<ToppingModel>> GetToppingsAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/toppings/active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ToppingModel>>();
        }

        /// <summary>Danh sách combo gia đình đang bán.</summary>
        public async Task<List<ComboModel>> GetCombosAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/combos/active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ComboModel>>();
        }

        /// <summary>Id sản phẩm xếp theo số lượng đã bán (cho kiểu sắp xếp "Bán chạy").</summary>
        public async Task<List<Guid>> GetBestSellerIdsAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/products/best-sellers?top=100");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<Guid>>();
        }

        /// <summary>
        /// BA shop item 4: tổng số lượng đã bán theo từng sản phẩm, hiển thị
        /// "Đã bán N suất" trên thẻ sản phẩm (social proof). Fail-soft.
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
        /// Wave 6b (item 26): điểm đánh giá tổng hợp theo sản phẩm
        /// {ProductId, AverageRating, ReviewCount}. Fail-soft: trang shop
        /// ẩn sao khi gọi này lỗi.
        /// </summary>
        public async Task<List<ProductRatingModel>> GetProductRatingsAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/products/ratings");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ProductRatingModel>>()
                ?? new List<ProductRatingModel>();
        }

        /// <summary>Số suất đã bán hôm nay ("Đã bán N gói hôm nay").</summary>
        public async Task<long> GetTodaySoldAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/dashboard/today-sold");
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadAs<TodaySoldModel>();
            return model?.QuantitySold ?? 0;
        }

        /// <summary>
        /// Item 165: trạng thái giới hạn suất theo ngày của món giới hạn
        /// ("Còn N suất"). Fail-soft — shop ẩn badge khi gọi lỗi.
        /// </summary>
        public async Task<List<ProductDailyRemainingModel>> GetDailyRemainingAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/products/daily-remaining");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ProductDailyRemainingModel>>()
                ?? new List<ProductDailyRemainingModel>();
        }

        /// <summary>Gửi yêu cầu báo giá đặt tiệc / số lượng lớn. Không thanh toán ở bước này.</summary>
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

        /// <summary>Đăng ký SĐT nhận nhắc cúng hàng tháng (public).</summary>
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
        /// Tên/địa chỉ của đơn gần nhất theo SĐT (để checkout tự điền).
        /// Trả null khi không có đơn cũ nào có địa chỉ.
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
        /// Item 143: voucher đơn đầu tiên cho khách mới. Trả null khi gọi
        /// lỗi hoặc SĐT không hợp lệ (fail-soft).
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

        /// <summary>Thông tin chuyển khoản ngân hàng public (tài khoản + cấu hình VietQR).</summary>
        public async Task<BankTransferInfoModel> GetBankTransferInfoAsync()
        {
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync("api/payments/bank-transfer-info");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<BankTransferInfoModel>();
        }

        /// <summary>Khách báo đã chuyển khoản (SĐT phải khớp với SĐT của đơn).</summary>
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

        /// <summary>Tạo thanh toán MoMo (trang test giả lập trả payUrl khi đang bật TestMode).</summary>
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

        /// <summary>Gửi MoMo return/callback (trang giả lập TEST dùng).</summary>
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

        /// <summary>Tra cứu đơn hàng public theo mã đơn + SĐT.</summary>
        public async Task<TrackedOrderModel> TrackOrderAsync(string orderNumber, string phone)
        {
            var url = $"api/orders/track?orderNumber={Uri.EscapeDataString(orderNumber ?? string.Empty)}"
                + $"&phone={Uri.EscapeDataString(phone ?? string.Empty)}";
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<TrackedOrderModel>();
        }

        /// <summary>Public: lịch sử đơn đầy đủ theo SĐT VN (không cần đăng nhập).</summary>
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
