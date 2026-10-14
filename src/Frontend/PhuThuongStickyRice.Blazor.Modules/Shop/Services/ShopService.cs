using PhuThuongStickyRice.Blazor.Modules.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
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

        /// <summary>Validates a single product id (used when importing a shared cart).</summary>
        public async Task<ShopProductModel> GetProductAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"api/products/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<ShopProductModel>();
        }

        /// <summary>Active toppings for the quick-view checkboxes.</summary>
        public async Task<List<ToppingModel>> GetToppingsAsync()
        {
            var response = await _httpClient.GetAsync("api/toppings/active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ToppingModel>>();
        }

        /// <summary>Active family combos for the shop.</summary>
        public async Task<List<ComboModel>> GetCombosAsync()
        {
            var response = await _httpClient.GetAsync("api/combos/active");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<ComboModel>>();
        }

        /// <summary>Product ids ordered by quantity sold (for "Bán chạy" sort).</summary>
        public async Task<List<Guid>> GetBestSellerIdsAsync()
        {
            var response = await _httpClient.GetAsync("api/products/best-sellers?top=100");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<Guid>>();
        }

        /// <summary>Quantity of items sold today ("Đã bán N gói hôm nay").</summary>
        public async Task<long> GetTodaySoldAsync()
        {
            var response = await _httpClient.GetAsync("api/dashboard/today-sold");
            response.EnsureSuccessStatusCode();
            var model = await response.Content.ReadAs<TodaySoldModel>();
            return model?.QuantitySold ?? 0;
        }

        /// <summary>Submits a bulk/event quote request (đặt tiệc). No payment.</summary>
        public async Task CreateQuoteAsync(QuoteRequestModel quote)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/quotes");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = quote.AsJsonContent();

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Active dish candidates ordered by votes.</summary>
        public async Task<List<DishCandidateModel>> GetDishCandidatesAsync()
        {
            var response = await _httpClient.GetAsync("api/dish-candidates");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<List<DishCandidateModel>>();
        }

        /// <summary>Votes for a dish candidate.</summary>
        public async Task<DishCandidateModel> VoteDishCandidateAsync(Guid id)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/dish-candidates/{id}/vote");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<DishCandidateModel>();
        }

        /// <summary>
        /// Last order name/address for a phone number (for checkout auto-fill).
        /// Returns null when no previous order with an address exists.
        /// </summary>
        public async Task<LastOrderInfoModel> GetLastOrderInfoAsync(string phone)
        {
            var response = await _httpClient.GetAsync(
                $"api/orders/last-by-phone?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<LastOrderInfoModel>();
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

        /// <summary>Public bank-transfer details (account info + VietQR config).</summary>
        public async Task<BankTransferInfoModel> GetBankTransferInfoAsync()
        {
            var response = await _httpClient.GetAsync("api/payments/bank-transfer-info");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<BankTransferInfoModel>();
        }

        /// <summary>Customer reports they sent the bank transfer (phone must match the order).</summary>
        public async Task<TrackedOrderModel> MarkTransferredAsync(Guid orderId, string phone)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/orders/{orderId}/mark-transferred");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new { CustomerPhone = phone }.AsJsonContent();

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<TrackedOrderModel>();
        }

        /// <summary>Creates a MoMo payment (test simulator payUrl while TestMode is on).</summary>
        public async Task<MoMoCreatePaymentResultModel> CreateMoMoPaymentAsync(Guid orderId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/orders/{orderId}/momo/create");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<MoMoCreatePaymentResultModel>();
        }

        /// <summary>Posts a MoMo return/callback (used by the TEST simulator page).</summary>
        public async Task MoMoCallbackAsync(MoMoCallbackModel callback)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/momo/callback");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = callback.AsJsonContent();

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Public order lookup by order number + phone.</summary>
        public async Task<TrackedOrderModel> TrackOrderAsync(string orderNumber, string phone)
        {
            var url = $"api/orders/track?orderNumber={Uri.EscapeDataString(orderNumber ?? string.Empty)}"
                + $"&phone={Uri.EscapeDataString(phone ?? string.Empty)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<TrackedOrderModel>();
        }
    }
}
