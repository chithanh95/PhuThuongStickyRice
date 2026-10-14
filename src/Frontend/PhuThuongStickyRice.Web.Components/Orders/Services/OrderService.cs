using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Orders.Services
{
    public class OrderService : HttpService
    {
        public OrderService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<Orders.Models.PagedResult<OrderModel>> GetOrdersAsync(int page = 1, int pageSize = 20,
            string searchTerm = null, string status = null, Guid? shipperId = null,
            DateTime? fromDate = null, DateTime? toDate = null)
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

            // Wave 6c (item 35): date-only range filter.
            if (fromDate.HasValue)
            {
                url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
            }

            if (toDate.HasValue)
            {
                url += $"&toDate={toDate.Value:yyyy-MM-dd}";
            }

            return await GetAsync<Orders.Models.PagedResult<OrderModel>>(url);
        }

        /// <summary>
        /// Wave 6c (item 32): URL of the Excel export honoring the same
        /// filters as the orders list. Downloaded via interop.downloadFile
        /// (bearer token auth) because a plain anchor cannot set headers.
        /// </summary>
        public string GetExportOrdersUrl(string searchTerm = null, string status = null,
            Guid? shipperId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                parts.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                parts.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (shipperId.HasValue && shipperId.Value != Guid.Empty)
            {
                parts.Add($"shipperId={shipperId.Value:D}");
            }

            if (fromDate.HasValue)
            {
                parts.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            }

            if (toDate.HasValue)
            {
                parts.Add($"toDate={toDate.Value:yyyy-MM-dd}");
            }

            var query = parts.Count == 0 ? "" : "?" + string.Join("&", parts);
            var baseAddress = _httpClient.BaseAddress?.ToString()?.TrimEnd('/');
            return $"{baseAddress}/api/admin/orders/export{query}";
        }

        /// <summary>Wave 6c (item 31): daily payment-channel reconciliation.</summary>
        public async Task<DailyReconciliationModel> GetReconciliationAsync(DateTime? date = null)
        {
            var url = "api/admin/orders/reconciliation";
            if (date.HasValue)
            {
                url += $"?date={date.Value:yyyy-MM-dd}";
            }

            return await GetAsync<DailyReconciliationModel>(url);
        }

        /// <summary>Wave 6c (item 33): admin sets/clears the internal note.</summary>
        public async Task<OrderModel> UpdateAdminNoteAsync(Guid orderId, string note)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/admin-note",
                new { Note = note });
        }

        /// <summary>Wave 6c (item 34): status-transition timeline, oldest first.</summary>
        public async Task<List<OrderStatusHistoryModel>> GetStatusHistoryAsync(Guid orderId)
        {
            return await GetAsync<List<OrderStatusHistoryModel>>($"api/orders/{orderId}/status-history");
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
