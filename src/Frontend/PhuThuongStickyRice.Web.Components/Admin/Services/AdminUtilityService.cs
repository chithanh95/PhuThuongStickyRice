using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Items 157–158: admin global search + notification center.
    /// </summary>
    public class AdminUtilityService : HttpService
    {
        public AdminUtilityService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<GlobalSearchResultModel> SearchAsync(string q)
        {
            return await GetAsync<GlobalSearchResultModel>(
                $"api/admin/search?q={System.Uri.EscapeDataString(q ?? string.Empty)}");
        }

        public async Task<List<NotificationModel>> GetNotificationsAsync()
        {
            return await GetAsync<List<NotificationModel>>("api/admin/notifications");
        }
    }
}
