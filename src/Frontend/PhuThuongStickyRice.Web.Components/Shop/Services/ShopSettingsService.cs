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

        public async Task<ShopSettingModel> UpdateAsync(ShopSettingModel model)
        {
            return await PutAsync<ShopSettingModel>("api/shop-settings", model);
        }
    }
}
