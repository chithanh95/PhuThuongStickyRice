using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Item 164: shop-side APIs for prepaid cards (buy + lookup by phone).</summary>
    public class PrepaidCardService : HttpService
    {
        public PrepaidCardService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<PrepaidCardModel> BuyAsync(BuyPrepaidCardRequest request)
        {
            return await PostAsync<PrepaidCardModel>("api/prepaid-cards/buy", request);
        }

        public async Task<List<PrepaidCardModel>> GetMyCardsAsync(string phone)
        {
            return await GetAsync<List<PrepaidCardModel>>(
                $"api/prepaid-cards/my?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }
    }
}
