using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>Item 164: admin APIs for prepaid cards (list + redeem).</summary>
    public class PrepaidCardAdminService : HttpService
    {
        public PrepaidCardAdminService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        public async Task<List<PrepaidCardModel>> GetListAsync(string searchTerm, int skip = 0, int take = 20)
        {
            var url = $"api/prepaid-cards?skip={skip}&take={take}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<List<PrepaidCardModel>>(url);
        }

        public async Task<PrepaidCardModel> RedeemAsync(string cardCode)
        {
            return await PostAsync<PrepaidCardModel>(
                $"api/prepaid-cards/{Uri.EscapeDataString(cardCode ?? string.Empty)}/redeem");
        }
    }
}
