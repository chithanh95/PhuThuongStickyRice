using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Client gọi API mẫu tin nhắn theo trạng thái đơn hàng (item 162):
    /// lấy danh sách và cập nhật nội dung/kích hoạt từng mẫu.
    /// </summary>
    public class OrderMessageTemplateService : ApiClientBase
    {
        public OrderMessageTemplateService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Lấy danh sách mẫu tin nhắn theo trạng thái đơn hàng.</summary>
        public async Task<List<OrderMessageTemplateModel>> GetListAsync()
        {
            return await GetAsync<List<OrderMessageTemplateModel>>("api/admin/order-message-templates");
        }

        /// <summary>Cập nhật nội dung và trạng thái kích hoạt của một mẫu tin.</summary>
        /// <param name="id">Id mẫu tin.</param>
        /// <param name="template">Nội dung mẫu mới (hỗ trợ {placeholder}).</param>
        /// <param name="isActive">true = mẫu được dùng khi gửi tin.</param>
        public async Task<OrderMessageTemplateModel> UpdateAsync(Guid id, string template, bool isActive)
        {
            return await PutAsync<OrderMessageTemplateModel>(
                $"api/admin/order-message-templates/{id:D}",
                new { Template = template, IsActive = isActive });
        }
    }
}
