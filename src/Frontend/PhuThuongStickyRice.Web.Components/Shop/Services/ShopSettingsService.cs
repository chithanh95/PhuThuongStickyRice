using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Shop commerce settings (public GET, admin PUT).</summary>
    public class ShopSettingsService : HttpService
    {
        public ShopSettingsService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<ShopSettingModel> GetAsync()
        {
            return await GetAsync<ShopSettingModel>("api/shop-settings");
        }

        /// <summary>
        /// Item 117: storefront view — a full-day holiday today automatically
        /// pauses ordering with the holiday reason. The landing/shop/checkout
        /// pages use this instead of <see cref="GetAsync"/>.
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
            public bool Ok { get; set; }
            public string Message { get; set; }
        }
    }
}
