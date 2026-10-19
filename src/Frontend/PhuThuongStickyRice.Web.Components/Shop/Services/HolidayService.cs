using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>Item 117: holiday schedule (public upcoming + admin CRUD).</summary>
    public class HolidayService : ApiClientBase
    {
        /// <summary>
        /// Khởi tạo service lịch nghỉ lễ (item 117).
        /// </summary>
        public HolidayService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Public: upcoming holidays in the next 30 days.</summary>
        public async Task<List<HolidayModel>> GetUpcomingAsync()
        {
            return await GetAsync<List<HolidayModel>>("api/shop/holidays")
                ?? new List<HolidayModel>();
        }

        /// <summary>
        /// Danh sách ngày nghỉ cho trang admin quản lý (không giới hạn 30 ngày như bản public).
        /// </summary>
        /// <returns>Danh sách ngày nghỉ; rỗng nếu lỗi.</returns>
        public async Task<List<HolidayModel>> GetListAsync()
        {
            return await GetAsync<List<HolidayModel>>("api/shop/admin/holidays")
                ?? new List<HolidayModel>();
        }

        /// <summary>
        /// Tạo ngày nghỉ mới. Nếu là nghỉ cả ngày (full-day), server tự tạm dừng
        /// nhận đơn trong ngày đó nên shop không cần thao tác thêm.
        /// </summary>
        /// <param name="model">Thông tin ngày nghỉ.</param>
        /// <returns>Ngày nghỉ vừa tạo.</returns>
        public async Task<HolidayModel> CreateAsync(CreateHolidayModel model)
        {
            return await PostAsync<HolidayModel>("api/shop/admin/holidays", model);
        }

        /// <summary>
        /// Cập nhật ngày nghỉ (đổi full-day/giờ nghỉ thì server tính lại trạng thái nhận đơn).
        /// </summary>
        /// <param name="id">Id ngày nghỉ.</param>
        /// <param name="model">Thông tin mới.</param>
        /// <returns>Ngày nghỉ sau khi cập nhật.</returns>
        public async Task<HolidayModel> UpdateAsync(Guid id, CreateHolidayModel model)
        {
            return await PutAsync<HolidayModel>($"api/shop/admin/holidays/{id}", model);
        }

        /// <summary>
        /// Xóa ngày nghỉ — quán nhận đơn trở lại bình thường cho ngày đó.
        /// </summary>
        /// <param name="id">Id ngày nghỉ cần xóa.</param>
        public async Task DeleteAsync(Guid id)
        {
            await SetBearerToken();
            using var response = await _httpClient.DeleteAsync($"api/shop/admin/holidays/{id}");
            response.EnsureSuccessStatusCode();
        }
    }
}
