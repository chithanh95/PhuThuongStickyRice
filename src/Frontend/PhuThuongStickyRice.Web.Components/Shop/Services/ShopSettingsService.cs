using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Shop commerce settings (public GET, admin PUT).</summary>
    public class ShopSettingsService : HttpService
    {
        public ShopSettingsService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Admin-only (GET api/shop-settings requires Admin role): full
        /// settings including integration secrets for the settings form.
        /// Anonymous storefront code must use <see cref="GetPublicAsync"/>.
        /// </summary>
        public async Task<ShopSettingModel> GetAsync()
        {
            return await GetAsync<ShopSettingModel>("api/shop-settings");
        }

        /// <summary>
        /// Item 117: storefront view — a full-day holiday today automatically
        /// pauses ordering with the holiday reason. The landing/shop/checkout
        /// pages use this instead of <see cref="GetAsync"/>. Integration
        /// secrets are stripped server-side on this endpoint.
        /// </summary>
        public async Task<ShopSettingModel> GetPublicAsync()
        {
            return await GetAsync<ShopSettingModel>("api/shop-settings/public");
        }

        public async Task<ShopSettingModel> UpdateAsync(ShopSettingModel model)
        {
            return await PutAsync<ShopSettingModel>("api/shop-settings", model);
        }

        /// <summary>
        /// Item 180: "Gửi thử" — posts a sample new-order payload.
        /// </summary>
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
