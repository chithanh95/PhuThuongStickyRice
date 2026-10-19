using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Orders.Services
{
    /// <summary>
    /// Item 130 (đúng): VIP customer marking and priority.
    /// Calls api/admin/customer-care/vip (Admin/Cashier).
    /// </summary>
    public class VipCustomerService : ApiClientBase
    {
        public VipCustomerService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Lấy danh sách tất cả khách hàng đang được gắn dấu VIP.</summary>
        public async Task<List<CustomerTagModel>> GetVipCustomersAsync()
        {
            return await GetAsync<List<CustomerTagModel>>("api/admin/customer-care/vip")
                ?? new List<CustomerTagModel>();
        }

        /// <summary>Marks a phone VIP (idempotent).</summary>
        public async Task<CustomerTagModel> MarkVipAsync(string phone)
        {
            return await PostAsync<CustomerTagModel>(
                $"api/admin/customer-care/vip/{Uri.EscapeDataString(phone ?? "")}",
                new { });
        }

        /// <summary>Removes the VIP mark from a phone.</summary>
        public async Task UnmarkVipAsync(string phone)
        {
            await DeleteAsync(
                $"api/admin/customer-care/vip/{Uri.EscapeDataString(phone ?? "")}");
        }
    }
}
