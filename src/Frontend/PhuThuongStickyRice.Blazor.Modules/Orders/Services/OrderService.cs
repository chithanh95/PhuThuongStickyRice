using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Orders.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Orders.Services
{
    public class OrderService : HttpService
    {
        public OrderService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<PagedResult<OrderModel>> GetOrdersAsync(int page = 1, int pageSize = 20,
            string searchTerm = null, string status = null)
        {
            var url = $"api/orders?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                url += $"&status={Uri.EscapeDataString(status)}";
            }

            return await GetAsync<PagedResult<OrderModel>>(url);
        }

        public async Task<OrderModel> UpdateStatusAsync(Guid orderId, string status)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/status",
                new UpdateOrderStatusModel { Status = status });
        }

        /// <summary>Admin confirms a bank-transfer payment.</summary>
        public async Task<OrderModel> ConfirmPaymentAsync(Guid orderId)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/confirm-payment", new { });
        }
    }
}
