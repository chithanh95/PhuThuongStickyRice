using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Item 117: holiday schedule (public upcoming + admin CRUD).</summary>
    public class HolidayService : HttpService
    {
        public HolidayService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        /// <summary>Public: upcoming holidays in the next 30 days.</summary>
        public async Task<List<HolidayModel>> GetUpcomingAsync()
        {
            return await GetAsync<List<HolidayModel>>("api/shop/holidays")
                ?? new List<HolidayModel>();
        }

        public async Task<List<HolidayModel>> GetListAsync()
        {
            return await GetAsync<List<HolidayModel>>("api/shop/admin/holidays")
                ?? new List<HolidayModel>();
        }

        public async Task<HolidayModel> CreateAsync(CreateHolidayModel model)
        {
            return await PostAsync<HolidayModel>("api/shop/admin/holidays", model);
        }

        public async Task<HolidayModel> UpdateAsync(Guid id, CreateHolidayModel model)
        {
            return await PutAsync<HolidayModel>($"api/shop/admin/holidays/{id}", model);
        }

        public async Task DeleteAsync(Guid id)
        {
            await SetBearerToken();
            var response = await _httpClient.DeleteAsync($"api/shop/admin/holidays/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
