using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Customer's saved addresses (api/customer/addresses). Requires login;
    /// the JWT is attached by <see cref="HttpService"/>.
    /// </summary>
    public class CustomerAddressService : HttpService
    {
        public CustomerAddressService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<List<CustomerAddressModel>> GetMineAsync()
        {
            try
            {
                return await GetAsync<List<CustomerAddressModel>>("api/customer/addresses")
                    ?? new List<CustomerAddressModel>();
            }
            catch (Exception)
            {
                return new List<CustomerAddressModel>();
            }
        }

        public async Task<CustomerAddressModel> CreateAsync(CustomerAddressModel model)
        {
            await SetBearerToken();
            var response = await _httpClient.PostAsJsonAsync("api/customer/addresses", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomerAddressModel>();
        }

        public async Task<CustomerAddressModel> UpdateAsync(Guid id, CustomerAddressModel model)
        {
            await SetBearerToken();
            var response = await _httpClient.PutAsJsonAsync($"api/customer/addresses/{id}", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomerAddressModel>();
        }

        public async Task DeleteAsync(Guid id)
        {
            await SetBearerToken();
            var response = await _httpClient.DeleteAsync($"api/customer/addresses/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task SetDefaultAsync(Guid id)
        {
            await SetBearerToken();
            var response = await _httpClient.PostAsync($"api/customer/addresses/{id}/default", null);
            response.EnsureSuccessStatusCode();
        }
    }

    public class CustomerAddressModel
    {
        public Guid Id { get; set; }

        public string Label { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public string AddressDetail { get; set; }

        public string Note { get; set; }

        public bool IsDefault { get; set; }
    }
}
