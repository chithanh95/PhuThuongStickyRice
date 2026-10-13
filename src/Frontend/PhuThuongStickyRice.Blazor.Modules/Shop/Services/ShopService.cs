using PhuThuongStickyRice.Blazor.Modules.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Shop.Services
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

        public async Task<PagedResult<ShopProductModel>> GetProductsAsync()
        {
            var response = await _httpClient.GetAsync("api/products?page=1&pageSize=100&status=Active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<PagedResult<ShopProductModel>>();
        }

        public async Task<PlacedOrderModel> CreateOrderAsync(CreateOrderModel order)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/orders");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = order.AsJsonContent();

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<PlacedOrderModel>();
        }
    }
}
