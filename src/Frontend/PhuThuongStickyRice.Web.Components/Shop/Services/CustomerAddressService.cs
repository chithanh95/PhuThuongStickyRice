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
    /// the JWT is attached by <see cref="ApiClientBase"/>.
    /// </summary>
    public class CustomerAddressService : ApiClientBase
    {
        public CustomerAddressService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Lấy danh sách địa chỉ đã lưu của khách đang đăng nhập.
        /// Lỗi mạng/API thì trả danh sách rỗng để shop vẫn chạy được.
        /// </summary>
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

        /// <summary>Thêm một địa chỉ nhận hàng mới cho khách đang đăng nhập.</summary>
        public async Task<CustomerAddressModel> CreateAsync(CustomerAddressModel model)
        {
            await SetBearerToken();
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.PostAsJsonAsync("api/customer/addresses", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomerAddressModel>();
        }

        /// <summary>Cập nhật địa chỉ đã lưu theo id.</summary>
        public async Task<CustomerAddressModel> UpdateAsync(Guid id, CustomerAddressModel model)
        {
            await SetBearerToken();
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.PutAsJsonAsync($"api/customer/addresses/{id}", model);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CustomerAddressModel>();
        }

        /// <summary>Xóa địa chỉ đã lưu theo id.</summary>
        public async Task DeleteAsync(Guid id)
        {
            await SetBearerToken();
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.DeleteAsync($"api/customer/addresses/{id}");
            response.EnsureSuccessStatusCode();
        }

        /// <summary>Đặt một địa chỉ làm mặc định.</summary>
        public async Task SetDefaultAsync(Guid id)
        {
            await SetBearerToken();
            // Dispose response để trả kết nối HTTP về pool (CA2000).
            using var response = await _httpClient.PostAsync($"api/customer/addresses/{id}/default", null);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Model địa chỉ nhận hàng đã lưu của khách (api/customer/addresses) —
    /// dùng cho checkout chọn địa chỉ có sẵn và màn sổ địa chỉ.
    /// </summary>
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
