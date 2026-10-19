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
    public class InventoryService : ApiClientBase
    {
        public InventoryService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Lưu kết quả kiểm kê cuối ngày của admin.</summary>
        /// <param name="model">Số lượng tồn thực tế theo từng món/nguyên liệu.</param>
        public async Task SaveInventoryAsync(SaveInventoryModel model)
        {
            await PostAsync<object>("api/admin/inventory", model);
        }

        /// <summary>Lấy lịch sử kiểm kê N ngày gần nhất (mặc định 30 ngày).</summary>
        /// <param name="days">Số ngày cần lấy.</param>
        public async Task<List<InventoryHistoryDayModel>> GetHistoryAsync(int days = 30)
        {
            return await GetAsync<List<InventoryHistoryDayModel>>(
                $"api/admin/inventory/history?days={days}");
        }
    }
}
