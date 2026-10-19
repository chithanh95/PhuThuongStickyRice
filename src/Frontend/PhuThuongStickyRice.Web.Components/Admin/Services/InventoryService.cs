using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Item 87: end-of-day inventory counts (kiểm kê cuối ngày).
    /// </summary>
    public class InventoryService : HttpService
    {
        public InventoryService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task SaveInventoryAsync(SaveInventoryModel model)
        {
            await PostAsync<object>("api/admin/inventory", model);
        }

        public async Task<List<InventoryHistoryDayModel>> GetHistoryAsync(int days = 30)
        {
            return await GetAsync<List<InventoryHistoryDayModel>>(
                $"api/admin/inventory/history?days={days}");
        }
    }
}
