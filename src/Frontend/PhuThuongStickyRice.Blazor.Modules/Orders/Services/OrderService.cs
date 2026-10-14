using PhuThuongStickyRice.Blazor.Modules.Admin.Models;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Orders.Models;
using PhuThuongStickyRice.Blazor.Modules.Shop.Models;
using System;
using System.Collections.Generic;
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

        public async Task<Orders.Models.PagedResult<OrderModel>> GetOrdersAsync(int page = 1, int pageSize = 20,
            string searchTerm = null, string status = null, Guid? shipperId = null)
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

            if (shipperId.HasValue && shipperId.Value != Guid.Empty)
            {
                url += $"&shipperId={shipperId.Value:D}";
            }

            return await GetAsync<Orders.Models.PagedResult<OrderModel>>(url);
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

        /// <summary>Admin: get a single order by id (used by the print page).</summary>
        public async Task<OrderModel> GetByIdAsync(Guid orderId)
        {
            return await GetAsync<OrderModel>($"api/orders/{orderId}");
        }

        /// <summary>Admin: customers aggregated by phone number.</summary>
        public async Task<List<CustomerModel>> GetCustomersAsync(string searchTerm = null)
        {
            var url = "api/orders/customers";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"?searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<List<CustomerModel>>(url);
        }

        /// <summary>Item 82: assigns a shipper to an order (null unassigns).</summary>
        public async Task<OrderModel> AssignShipperAsync(Guid orderId, Guid? shipperId)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/assign-shipper",
                new { ShipperId = shipperId });
        }

        /// <summary>Item 87: admin hides/unhides an order review.</summary>
        public async Task<OrderReviewModel> SetReviewHiddenAsync(Guid reviewId, bool isHidden)
        {
            return await PutAsync<OrderReviewModel>($"api/orders/reviews/{reviewId}/hide",
                new { IsHidden = isHidden });
        }

        /// <summary>Item 87: admin replies to an order review.</summary>
        public async Task<OrderReviewModel> ReplyReviewAsync(Guid reviewId, string reply)
        {
            return await PutAsync<OrderReviewModel>($"api/orders/reviews/{reviewId}/reply",
                new { Reply = reply });
        }
    }
}
