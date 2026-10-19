using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Landing.Models;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Landing.Services
{
    public class LandingService : ApiClientBase
    {
        /// <summary>
        /// TTL cache client-side cho nội dung landing public: nội dung đổi
        /// hiếm khi nhưng endpoint được gọi ở mọi trang có ShopFooter/ShopLayout
        /// mỗi lần mở trang. Cùng pattern <c>ShopSettingsService</c>: static
        /// field vì service đăng ký Transient (mỗi page inject instance riêng).
        /// Caller chỉ đọc model (không mutate) nên trả cùng instance là an toàn.
        /// </summary>
        private static readonly TimeSpan ContentCacheTtl = TimeSpan.FromMinutes(2);
        private static readonly SemaphoreSlim ContentCacheLock = new(1, 1);
        private static LandingContentModel? _cachedContent;
        private static DateTimeOffset _cachedContentAt;

        public LandingService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Nội dung landing public. Kết quả được cache client-side 2 phút
        /// (static): cắt ~1 HTTP round-trip trên mỗi trang mở (footer + layout
        /// đều gọi). Admin lưu nội dung qua <see cref="UpdateContentAsync"/>
        /// sẽ xóa cache để lần đọc sau lấy dữ liệu mới ngay.
        /// </summary>
        public async Task<LandingContentModel> GetContentAsync()
        {
            var cached = _cachedContent;
            if (cached is not null && DateTimeOffset.UtcNow - _cachedContentAt < ContentCacheTtl)
                return cached;

            // Lock tránh N request cùng fetch khi cache hết hạn (chỉ 1 fetch).
            await ContentCacheLock.WaitAsync();
            try
            {
                // Kiểm tra lại sau khi vào lock: request khác có thể đã refresh xong.
                cached = _cachedContent;
                if (cached is not null && DateTimeOffset.UtcNow - _cachedContentAt < ContentCacheTtl)
                    return cached;

                var fresh = await GetAsync<LandingContentModel>("api/landing/content");
                if (fresh is not null)
                {
                    _cachedContent = fresh;
                    _cachedContentAt = DateTimeOffset.UtcNow;
                    return fresh;
                }
                // Fetch trả null: không ghi đè cache cũ (nếu có), giữ hành vi cũ.
                return cached!;
            }
            finally
            {
                ContentCacheLock.Release();
            }
        }

        /// <summary>Item 93: published news articles for the landing page.</summary>
        public async Task<List<ArticleModel>> GetArticlesAsync()
        {
            return await GetAsync<List<ArticleModel>>("api/articles/published");
        }

        /// <summary>Item 87: public non-hidden order reviews for the landing page.</summary>
        public async Task<List<PublicReviewModel>> GetPublicReviewsAsync()
        {
            return await GetAsync<List<PublicReviewModel>>("api/orders/reviews/public");
        }

        /// <summary>Thống kê landing: tổng đơn, điểm TB, số đánh giá.</summary>
        public async Task<LandingStatsModel> GetStatsAsync()
        {
            return await GetAsync<LandingStatsModel>("api/landing/stats");
        }

        /// <summary>Ảnh check-in đã duyệt cho gallery landing.</summary>
        public async Task<List<GalleryPhotoModel>> GetGalleryAsync()
        {
            return await GetAsync<List<GalleryPhotoModel>>("api/landing/gallery");
        }

        /// <summary>Đăng ký nhận ưu đãi qua SĐT.</summary>
        public async Task<bool> SubscribeAsync(string phone)
        {
            var response = await PostAsync<object>("api/landing/subscribe", new { phone });
            return response != null;
        }

        /// <summary>FAQ public cho landing.</summary>
        public async Task<List<Landing.Models.FaqPublicModel>> GetFaqsAsync()
        {
            return await GetAsync<List<Landing.Models.FaqPublicModel>>("api/faqs");
        }

        /// <summary>Lịch nghỉ sắp tới (banner lễ/Tết, item 20).</summary>
        public async Task<List<HolidayModel>> GetUpcomingHolidaysAsync()
        {
            return await GetAsync<List<HolidayModel>>("api/shop/holidays");
        }

        /// <summary>Vòng quay may mắn (item 28).</summary>
        public async Task<LuckySpinResultModel> SpinAsync(string phone)
        {
            return await PostAsync<LuckySpinResultModel>("api/landing/spin", new { phone });
        }

        /// <summary>Giới thiệu bạn bè (item 29).</summary>
        public async Task<bool> ReferFriendAsync(string referrerPhone, string referredPhone)
        {
            var response = await PostAsync<object>("api/landing/referral", new { referrerPhone, referredPhone });
            return response != null;
        }

        /// <summary>Chấm sao nhanh (item 36).</summary>
        public async Task<bool> SubmitQuickRatingAsync(int rating, Guid? productId = null)
        {
            var response = await PostAsync<object>("api/landing/quick-rating", new { rating, productId });
            return response != null;
        }

        /// <summary>Chat với quán (item 37).</summary>
        public async Task<bool> SendChatAsync(string sessionId, string message)
        {
            var response = await PostAsync<object>("api/landing/chat", new { sessionId, message });
            return response != null;
        }

        public async Task<List<ChatMessageModel>> GetChatMessagesAsync(string sessionId)
        {
            return await GetAsync<List<ChatMessageModel>>($"api/landing/chat/{sessionId}");
        }

        /// <summary>Off-peak promo (item 48).</summary>
        public async Task<OffPeakPromoModel> GetOffPeakAsync()
        {
            // bug-hunt 2026-10-19: BE trả ARRAY (Ok(promos)), FE cũ đọc OBJECT
            // -> JsonException bị catch {} nuốt ở Index.razor, banner "khung giờ
            // vắng khách" không bao giờ hiện. Đọc list rồi lấy promo đang diễn ra
            // (BE đã tính sẵn isNow theo giờ VN), khớp đúng kỳ vọng của UI.
            var promos = await GetAsync<List<OffPeakPromoModel>>("api/landing/offpeak");
            return promos?.FirstOrDefault(p => p != null && p.IsNow);
        }

        /// <summary>Điểm danh thử thách 7 ngày (item 53).</summary>
        public async Task<StreakCheckInResult> CheckInAsync(string phone)
        {
            return await PostAsync<StreakCheckInResult>("api/landing/checkin", new { phone });
        }

        /// <summary>Bình chọn món mới (item 54).</summary>
        public async Task<List<DishPollModel>> GetPollsAsync()
        {
            return await GetAsync<List<DishPollModel>>("api/landing/polls");
        }

        public async Task<bool> SubmitPollVoteAsync(Guid pollId, string phone)
        {
            var response = await PostAsync<object>($"api/landing/polls/{pollId}/vote", new { phone });
            return response != null;
        }

        /// <summary>Sinh nhật (item 43).</summary>
        public async Task<BirthdayGiftResult> ClaimBirthdayGiftAsync(string phone, DateTime birthDate)
        {
            return await PostAsync<BirthdayGiftResult>("api/landing/birthday-gift", new { phone, birthDate });
        }

        /// <summary>VIP status (item 55).</summary>
        public async Task<VipStatusModel> GetVipStatusAsync(string phone)
        {
            return await GetAsync<VipStatusModel>($"api/landing/vip-status?phone={phone}");
        }

        /// <summary>Khóa học (item 60).</summary>
        public async Task<List<CourseModel>> GetCoursesAsync()
        {
            return await GetAsync<List<CourseModel>>("api/landing/courses");
        }

        /// <summary>Parse ý định đặt hàng từ text (item 61/80).</summary>
        public async Task<ParseOrderResult> ParseOrderAsync(string text, string phone = null)
        {
            return await PostAsync<ParseOrderResult>("api/landing/parse-order", new { text, phone });
        }

        /// <summary>Món yêu thích (item 63).</summary>
        public async Task AddFavoriteAsync(string phone, Guid productId)
        {
            await PostAsync<object>("api/landing/favorites", new { phone, productId });
        }

        public async Task RemoveFavoriteAsync(string phone, Guid productId)
        {
            await DeleteAsync($"api/landing/favorites?phone={phone}&productId={productId}");
        }

        public async Task<List<Guid>> GetFavoritesAsync(string phone)
        {
            var wrapper = await GetAsync<ProductIdsApiResponse>($"api/landing/favorites?phone={phone}");
            return wrapper?.ProductIds ?? new List<Guid>();
        }

        public async Task<List<BackInStockItem>> GetBackInStockFavoritesAsync(string phone)
        {
            var wrapper = await GetAsync<ItemsApiResponse<BackInStockItem>>($"api/landing/favorites/back-in-stock?phone={phone}");
            return wrapper?.Items ?? new List<BackInStockItem>();
        }

        /// <summary>Items đơn gần nhất (item 62).</summary>
        public async Task<List<ParsedOrderItem>> GetLastOrderItemsAsync(string phone)
        {
            var wrapper = await GetAsync<ItemsApiResponse<ParsedOrderItem>>($"api/landing/last-order-items?phone={phone}");
            return wrapper?.Items ?? new List<ParsedOrderItem>();
        }

        /// <summary>Link sàn TMĐT (item 75).</summary>
        public async Task<MarketplaceLinksModel> GetMarketplaceLinksAsync()
        {
            return await GetAsync<MarketplaceLinksModel>("api/landing/marketplace-links");
        }

        public async Task<bool> EnrollCourseAsync(Guid id, string phone)
        {
            var response = await PostAsync<object>($"api/landing/courses/{id}/enroll", new { phone });
            return response != null;
        }

        public async Task<LandingContentModel> UpdateContentAsync(LandingContentModel model)
        {
            var result = await PutAsync<LandingContentModel>("api/landing/content", model);
            // Admin vừa lưu nội dung mới: xóa cache public để lần đọc sau
            // lấy dữ liệu mới ngay, không phải chờ cache hết hạn.
            InvalidateContentCache();
            return result;
        }

        /// <summary>
        /// Xóa cache client-side của <see cref="GetContentAsync"/> (vd sau khi
        /// admin lưu nội dung landing). Lần gọi sau sẽ fetch lại từ server.
        /// </summary>
        public static void InvalidateContentCache()
        {
            _cachedContent = null;
        }

        /// <summary>
        /// Uploads one image and returns its relative URL (e.g. /uploads/landing/xxx.jpg).
        /// Throws <see cref="InvalidOperationException"/> with the server message on validation failure.
        /// </summary>
        public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType)
        {
            await SetBearerToken();

            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            // HttpResponseMessage giữ connection/stream bên dưới: dispose ngay
            // sau khi đọc xong body để trả connection về pool.
            using var response = await _httpClient.PostAsync("api/landing/images", form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var message = "Tải ảnh lên thất bại.";
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msg))
                    {
                        message = msg.GetString() ?? message;
                    }
                }
                catch (JsonException)
                {
                    // Cố ý bỏ qua: body lỗi không phải JSON thì giữ message mặc
                    // định bên trên — lỗi gốc không quan trọng vì ngay sau đó
                    // vẫn throw InvalidOperationException cho người gọi xử lý.
                }

                throw new InvalidOperationException(message);
            }

            using var result = JsonDocument.Parse(body);
            if (result.RootElement.TryGetProperty("url", out var url))
            {
                return url.GetString();
            }

            throw new InvalidOperationException("Máy chủ không trả về đường dẫn ảnh.");
        }

        /// <summary>
        /// Builds the absolute URL for an image returned by the upload endpoint.
        /// The API serves uploads from its own wwwroot, so relative URLs like
        /// /uploads/landing/xxx.jpg are resolved against the API base address.
        /// </summary>
        public string ResolveImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');
            var path = url.StartsWith('/') ? url : "/" + url;
            return string.IsNullOrEmpty(baseUrl) ? path : baseUrl + path;
        }
    }
}
