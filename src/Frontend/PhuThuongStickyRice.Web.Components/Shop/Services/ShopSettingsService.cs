using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Shop commerce settings (public GET, admin PUT).</summary>
    public class ShopSettingsService : ApiClientBase
    {
        /// <summary>
        /// TTL cache client-side cho settings public: settings đổi hiếm khi nhưng
        /// endpoint được gọi ở ~14 trang/components mỗi lần mở trang.
        /// Cache nằm trên static field vì service đăng ký Transient qua AddHttpClient
        /// (mỗi page inject instance riêng) nên instance-field không chia sẻ được.
        /// Caller chỉ đọc model (không mutate) nên trả cùng instance là an toàn.
        /// </summary>
        private static readonly TimeSpan PublicSettingsCacheTtl = TimeSpan.FromMinutes(2);
        private static readonly SemaphoreSlim PublicSettingsCacheLock = new(1, 1);
        private static ShopSettingModel? _cachedPublicSettings;
        private static DateTimeOffset _cachedPublicSettingsAt;

        public ShopSettingsService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Admin-only (GET api/shop-settings requires Admin role): full
        /// settings including integration secrets for the settings form.
        /// Anonymous storefront code must use <see cref="GetPublicAsync"/>.
        /// </summary>
        public async Task<ShopSettingModel> GetAdminAsync()
        {
            return await GetAsync<ShopSettingModel>("api/shop-settings");
        }

        /// <summary>
        /// Item 117: storefront view — a full-day holiday today automatically
        /// pauses ordering with the holiday reason. The landing/shop/checkout
        /// pages use this instead of <see cref="GetAdminAsync"/>. Integration
        /// secrets are stripped server-side on this endpoint.
        /// Kết quả được cache client-side 2 phút (static, vì service là Transient):
        /// cắt ~1 HTTP round-trip trên mỗi trang mở mà dữ liệu không đổi.
        /// </summary>
        public async Task<ShopSettingModel> GetPublicAsync()
        {
            var cached = _cachedPublicSettings;
            if (cached is not null && DateTimeOffset.UtcNow - _cachedPublicSettingsAt < PublicSettingsCacheTtl)
                return cached;

            // Lock tránh N request cùng fetch khi cache hết hạn (chỉ 1 fetch).
            await PublicSettingsCacheLock.WaitAsync();
            try
            {
                // Kiểm tra lại sau khi vào lock: request khác có thể đã refresh xong.
                cached = _cachedPublicSettings;
                if (cached is not null && DateTimeOffset.UtcNow - _cachedPublicSettingsAt < PublicSettingsCacheTtl)
                    return cached;

                var fresh = await GetAsync<ShopSettingModel>("api/shop-settings/public");
                if (fresh is not null)
                {
                    _cachedPublicSettings = fresh;
                    _cachedPublicSettingsAt = DateTimeOffset.UtcNow;
                    return fresh;
                }
                // Fetch trả null: không ghi đè cache cũ (nếu có), giữ hành vi cũ.
                return cached!;
            }
            finally
            {
                PublicSettingsCacheLock.Release();
            }
        }

        /// <summary>
        /// Admin lưu cài đặt shop. Sau khi lưu phải xóa cache public
        /// (<see cref="GetPublicAsync"/> cache 2 phút) để shop/landing/checkout
        /// đọc ngay giá trị mới, không phải chờ cache hết hạn.
        /// </summary>
        /// <param name="model">Cài đặt cần lưu.</param>
        /// <returns>Cài đặt sau khi server lưu.</returns>
        public async Task<ShopSettingModel> UpdateAsync(ShopSettingModel model)
        {
            var result = await PutAsync<ShopSettingModel>("api/shop-settings", model);
            // Admin vừa lưu settings mới: xóa cache public để lần đọc sau lấy dữ liệu mới.
            InvalidatePublicCache();
            return result;
        }

        /// <summary>
        /// Xóa cache client-side của <see cref="GetPublicAsync"/> (vd sau khi admin
        /// lưu settings). Lần gọi sau sẽ fetch lại từ server.
        /// </summary>
        public static void InvalidatePublicCache()
        {
            _cachedPublicSettings = null;
        }

        /// <summary>
        /// Item 180: nút "Gửi thử" ở trang cài đặt — gửi 1 payload đơn hàng mẫu
        /// tới URL webhook admin nhập để kiểm tra tích hợp (vd Telegram/Zalo)
        /// có nhận được tin nhắn không, trước khi bật thật.
        /// </summary>
        /// <param name="url">URL webhook cần test.</param>
        /// <returns>Kết quả test: thành công hay không + message giải thích.</returns>
        public async Task<TestOrderWebhookResult> TestOrderWebhookAsync(string url)
        {
            return await PostAsync<TestOrderWebhookResult>(
                "api/shop-settings/test-order-webhook", new { Url = url });
        }

        public class TestOrderWebhookResult
        {
            public bool IsSuccess { get; set; }
            public string Message { get; set; }
        }
    }
}
