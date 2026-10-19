using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Item 162: message templates per order status (mẫu tin nhắn).
    /// </summary>
    public class MessageTemplateService : ApiClientBase
    {
        public MessageTemplateService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        public async Task<List<OrderMessageTemplateModel>> GetListAsync()
        {
            return await GetAsync<List<OrderMessageTemplateModel>>("api/admin/order-message-templates");
        }

        public async Task<OrderMessageTemplateModel> UpdateAsync(Guid id, string template, bool isActive)
        {
            return await PutAsync<OrderMessageTemplateModel>(
                $"api/admin/order-message-templates/{id:D}",
                new { Template = template, IsActive = isActive });
        }
    }
}
