using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Item 87: client gọi API kiểm kê cuối ngày.
    /// Admin chốt số lượng tồn thực tế theo từng món/nguyên liệu vào cuối
    /// ca để đối chiếu hao hụt — số liệu này là đầu vào cho báo cáo lãi gộp.
    /// </summary>
    public class InventoryService : ApiClientBase
    {
        /// <summary>Khởi tạo client gọi API kiểm kê.</summary>
        /// <param name="httpClient">HttpClient đã cấu hình base address API.</param>
        /// <param name="tokenStore">Kho token (chỉ admin đăng nhập mới kiểm kê).</param>
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
        /// <returns>Danh sách ngày kiểm kê theo thứ tự mới nhất trước.</returns>
        public async Task<List<InventoryHistoryDayModel>> GetHistoryAsync(int days = 30)
        {
            return await GetAsync<List<InventoryHistoryDayModel>>(
                $"api/admin/inventory/history?days={days}");
        }
    }
}
