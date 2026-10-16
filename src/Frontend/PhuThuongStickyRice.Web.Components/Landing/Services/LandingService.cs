using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Landing.Models;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Landing.Services
{
    public class LandingService : HttpService
    {
        public LandingService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<LandingContentModel> GetContentAsync()
        {
            return await GetAsync<LandingContentModel>("api/landing/content");
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
            var res = await PostAsync<object>("api/landing/subscribe", new { phone });
            return res != null;
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
        public async Task<bool> ReferAsync(string referrerPhone, string referredPhone)
        {
            var res = await PostAsync<object>("api/landing/referral", new { referrerPhone, referredPhone });
            return res != null;
        }

        /// <summary>Chấm sao nhanh (item 36).</summary>
        public async Task<bool> QuickRateAsync(int rating, Guid? productId = null)
        {
            var res = await PostAsync<object>("api/landing/quick-rating", new { rating, productId });
            return res != null;
        }

        /// <summary>Chat với quán (item 37).</summary>
        public async Task<bool> SendChatAsync(string sessionId, string message)
        {
            var res = await PostAsync<object>("api/landing/chat", new { sessionId, message });
            return res != null;
        }

        public async Task<List<ChatMessageModel>> GetChatMessagesAsync(string sessionId)
        {
            return await GetAsync<List<ChatMessageModel>>($"api/landing/chat/{sessionId}");
        }

        /// <summary>Off-peak promo (item 48).</summary>
        public async Task<OffPeakPromoModel> GetOffPeakAsync()
        {
            return await GetAsync<OffPeakPromoModel>("api/landing/offpeak");
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

        public async Task<bool> VotePollAsync(Guid pollId, string phone)
        {
            var res = await PostAsync<object>($"api/landing/polls/{pollId}/vote", new { phone });
            return res != null;
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

        public async Task<bool> EnrollCourseAsync(Guid id, string phone)
        {
            var res = await PostAsync<object>($"api/landing/courses/{id}/enroll", new { phone });
            return res != null;
        }

        public async Task<LandingContentModel> UpdateContentAsync(LandingContentModel model)
        {
            return await PutAsync<LandingContentModel>("api/landing/content", model);
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

            var response = await _httpClient.PostAsync("api/landing/images", form);
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
            var path = url.StartsWith("/") ? url : "/" + url;
            return string.IsNullOrEmpty(baseUrl) ? path : baseUrl + path;
        }
    }
}
