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
                throw new InvalidOperationException(await ReadErrorMessageAsync(response));
            }

            return await response.Content.ReadFromJsonAsync<CustomerProfileModel>();
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                // API returns { message: "..." } for user-friendly errors.
                var start = body.IndexOf("\"message\"", StringComparison.OrdinalIgnoreCase);
                if (start >= 0)
                {
                    var colon = body.IndexOf(':', start);
                    var quote1 = body.IndexOf('"', colon);
                    var quote2 = body.IndexOf('"', quote1 + 1);
                    if (quote1 >= 0 && quote2 > quote1)
                    {
                        return body.Substring(quote1 + 1, quote2 - quote1 - 1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(body) && body.Length < 300)
                {
                    return body;
                }
            }
            catch (Exception)
            {
                // Fall through to the generic message.
            }

            return "Lưu thất bại. Vui lòng thử lại.";
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
