using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Logged-in customer's profile: taste preferences + birthday
    /// (api/customer/profile). Requires login; JWT attached by HttpService.
    /// </summary>
    public class CustomerProfileService : HttpService
    {
        public CustomerProfileService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<CustomerProfileModel> GetMyAsync()
        {
            try
            {
                return await GetAsync<CustomerProfileModel>("api/customer/profile")
                    ?? new CustomerProfileModel();
            }
            catch (Exception)
            {
                return new CustomerProfileModel();
            }
        }

        public async Task<CustomerProfileModel> UpdateAsync(CustomerProfileModel model)
        {
            await SetBearerToken();
            var response = await _httpClient.PutAsJsonAsync("api/customer/profile", model);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(await HttpErrorMessageReader.ReadErrorMessageAsync(
                    response, "Lưu thất bại. Vui lòng thử lại."));
            }

            return await response.Content.ReadFromJsonAsync<CustomerProfileModel>();
        }
    }

    public class CustomerProfileModel
    {
        public List<string> TasteTags { get; set; } = new List<string>();

        public string TasteNote { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool CanChangeDateOfBirth { get; set; } = true;

        public string DateOfBirthChangeHint { get; set; }

        public string BirthdayGiftCode { get; set; }

        public decimal? BirthdayGiftAmount { get; set; }

        public DateTime? BirthdayGiftExpiresAt { get; set; }
    }
}
