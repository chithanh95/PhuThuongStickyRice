using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Items 157–158: tìm kiếm toàn cục + trung tâm thông báo của admin.
    /// (Tên cũ: AdminUtilityService — đổi để phản ánh đúng chức năng.)
    /// </summary>
    public class AdminSearchNotificationService : HttpService
    {
        public AdminSearchNotificationService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        /// <summary>
        /// Tìm kiếm toàn cục trong admin theo từ khóa.
        /// </summary>
        /// <param name="keyword">Từ khóa tìm kiếm.</param>
        /// <returns>Kết quả tìm kiếm gồm đơn hàng, sản phẩm, khách hàng.</returns>
        public async Task<GlobalSearchResultModel> SearchAsync(string keyword)
        {
            return await GetAsync<GlobalSearchResultModel>(
                $"api/admin/search?q={System.Uri.EscapeDataString(keyword ?? string.Empty)}");
        }

        /// <summary>
        /// Lấy danh sách thông báo mới của admin.
        /// </summary>
        /// <returns>Danh sách thông báo.</returns>
        public async Task<List<NotificationModel>> GetNotificationsAsync()
        {
            return await GetAsync<List<NotificationModel>>("api/admin/notifications");
        }
    }
}
