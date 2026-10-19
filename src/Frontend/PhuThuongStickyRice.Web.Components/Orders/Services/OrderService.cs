using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Orders.Services
{
    /// <summary>
    /// Client gọi API đơn hàng phía admin: liệt kê/lọc đơn, đổi trạng thái,
    /// thu tiền, đối soát, xuất báo cáo Excel, gộp đơn trùng, quản lý đánh giá
    /// và khách hàng. Server vẫn tự validate mọi nghiệp vụ; service này chỉ
    /// dựng URL và chuyển tiếp request.
    /// </summary>
    public class OrderService : ApiClientBase
    {
        public OrderService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Liệt kê đơn hàng có phân trang kèm 9 bộ lọc cho trang admin quản lý đơn.
        /// Chỉ param nào có giá trị mới được gắn vào query string.
        /// </summary>
        /// <param name="page">Trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số đơn mỗi trang.</param>
        /// <param name="searchTerm">Tìm theo mã đơn / tên / SĐT khách.</param>
        /// <param name="status">Lọc theo trạng thái đơn.</param>
        /// <param name="shipperId">Lọc theo shipper được gán.</param>
        /// <param name="fromDate">Từ ngày (yyyy-MM-dd).</param>
        /// <param name="toDate">Đến ngày (yyyy-MM-dd).</param>
        /// <param name="cashCollected">Lọc đơn đã/chưa thu tiền mặt.</param>
        /// <param name="vipOnly">True = chỉ đơn của khách VIP.</param>
        /// <returns>Trang kết quả đơn hàng.</returns>
        public async Task<Orders.Models.PagedResult<OrderModel>> GetOrdersAsync(int page = 1, int pageSize = 20,
            string searchTerm = null, string status = null, Guid? shipperId = null,
            DateTime? fromDate = null, DateTime? toDate = null, bool? cashCollected = null,
            bool vipOnly = false)
        {
            var url = $"api/orders?page={page}&pageSize={pageSize}";
            foreach (var part in BuildOrderFilterParts(searchTerm, status, shipperId, fromDate, toDate, cashCollected, vipOnly))
            {
                url += "&" + part;
            }

            return await GetAsync<Orders.Models.PagedResult<OrderModel>>(url);
        }

        /// <summary>
        /// Item 163: staff marks cash as collected ("Đã thu tiền").
        /// </summary>
        public async Task<OrderModel> MarkCashCollectedAsync(Guid orderId)
        {
            return await PostAsync<OrderModel>($"api/admin/orders/{orderId:D}/mark-cash-collected", new { });
        }

        /// <summary>
        /// Item 162: renders the message template for an order + status.
        /// </summary>
        public async Task<OrderMessagePreviewModel> GetMessagePreviewAsync(Guid orderId, string status)
        {
            var url = $"api/admin/orders/{orderId:D}/message-preview";
            if (!string.IsNullOrWhiteSpace(status))
            {
                url += $"?status={Uri.EscapeDataString(status)}";
            }

            return await GetAsync<OrderMessagePreviewModel>(url);
        }

        /// <summary>
        /// Wave 6c (item 32): URL of the Excel export honoring the same
        /// filters as the orders list. Downloaded via interop.downloadFile
        /// (bearer token auth) because a plain anchor cannot set headers.
        /// </summary>
        public string GetExportOrdersUrl(string searchTerm = null, string status = null,
            Guid? shipperId = null, DateTime? fromDate = null, DateTime? toDate = null,
            bool? cashCollected = null, bool vipOnly = false)
        {
            // Dùng chung với GetOrdersAsync: export phải khớp đúng bộ lọc
            // đang xem (SH5 từng thiếu cashCollected/vipOnly ở export).
            var parts = BuildOrderFilterParts(searchTerm, status, shipperId, fromDate, toDate, cashCollected, vipOnly);

            var query = parts.Count == 0 ? "" : "?" + string.Join("&", parts);
            // Dùng ApiBaseAddress ở ApiClientBase thay vì lặp TrimEnd('/') (codequality-team 2026-10-20).
            return $"{ApiBaseAddress}/api/admin/orders/export{query}";
        }

        /// <summary>
        /// Dựng các tham số query string của bộ lọc danh sách đơn (dùng chung
        /// cho <see cref="GetOrdersAsync"/> và <see cref="GetExportOrdersUrl"/>
        /// để export luôn khớp danh sách đang xem).
        /// </summary>
        /// <param name="searchTerm">Tìm theo mã đơn / tên / SĐT khách.</param>
        /// <param name="status">Lọc theo trạng thái đơn.</param>
        /// <param name="shipperId">Lọc theo shipper được gán.</param>
        /// <param name="fromDate">Từ ngày (yyyy-MM-dd).</param>
        /// <param name="toDate">Đến ngày (yyyy-MM-dd).</param>
        /// <param name="cashCollected">Lọc đơn đã/chưa thu tiền mặt.</param>
        /// <param name="vipOnly">True = chỉ đơn của khách VIP.</param>
        /// <returns>Danh sách "key=value" (chưa URL-prefix ?/&amp;).</returns>
        private static List<string> BuildOrderFilterParts(string searchTerm, string status,
            Guid? shipperId, DateTime? fromDate, DateTime? toDate,
            bool? cashCollected, bool vipOnly)
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

            // Wave 6c (item 35): date-only range filter.
            AddDateRangeParams(parts, fromDate, toDate, "fromDate", "toDate");

            // Item 163: cash-collection filter.
            if (cashCollected.HasValue)
            {
                parts.Add($"cashCollected={cashCollected.Value.ToString().ToLowerInvariant()}");
            }

            // Item 130 (đúng): only VIP customers' orders.
            if (vipOnly)
            {
                parts.Add("vipOnly=true");
            }

            return parts;
        }

        /// <summary>
        /// Thêm cặp tham số khoảng ngày (yyyy-MM-dd) vào danh sách query — dùng
        /// chung cho 4 method xuất/báo cáo trong class này để không viết lặp
        /// lại khối if from/to.
        /// </summary>
        /// <param name="parts">Danh sách tham số đang dựng.</param>
        /// <param name="from">Từ ngày (null = bỏ qua).</param>
        /// <param name="to">Đến ngày (null = bỏ qua).</param>
        /// <param name="fromKey">Tên tham số "from" của endpoint gọi.</param>
        /// <param name="toKey">Tên tham số "to" của endpoint gọi.</param>
        private static void AddDateRangeParams(List<string> parts, DateTime? from, DateTime? to,
            string fromKey = "from", string toKey = "to")
        {
            if (from.HasValue)
            {
                parts.Add($"{fromKey}={from.Value:yyyy-MM-dd}");
            }

            if (to.HasValue)
            {
                parts.Add($"{toKey}={to.Value:yyyy-MM-dd}");
            }
        }

        /// <summary>
        /// Revenue report (.xlsx, 2 sheets) for a Vietnam date range.
        /// </summary>
        public string GetExportRevenueUrl(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var parts = new List<string>();
            AddDateRangeParams(parts, fromDate, toDate, "fromDate", "toDate");

            var query = parts.Count == 0 ? "" : "?" + string.Join("&", parts);
            // Dùng ApiBaseAddress ở ApiClientBase thay vì lặp TrimEnd('/') (codequality-team 2026-10-20).
            return $"{ApiBaseAddress}/api/admin/orders/export-revenue{query}";
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

        /// <summary>Revenue forecast (moving averages + weekday pattern).</summary>
        public async Task<RevenueForecastModel> GetRevenueForecastAsync()
        {
            return await GetAsync<RevenueForecastModel>("api/admin/orders/revenue-forecast");
        }

        /// <summary>Item 99: hourly best-sellers report for one day.</summary>
        public async Task<HourlySalesReportModel> GetHourlySalesReportAsync(DateTime date)
        {
            // CA1305: format ngày cố định cho query string API — dùng
            // InvariantCulture để không phụ thuộc culture của máy client.
            var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return await GetAsync<HourlySalesReportModel>($"api/admin/orders/hourly-sales?date={dateString}");
        }

        /// <summary>Item 154: gross profit per product for a date range.</summary>
        public async Task<GrossProfitReportModel> GetGrossProfitReportAsync(DateTime? from, DateTime? to)
        {
            var url = "api/admin/reports/gross-profit";
            var queryParams = new List<string>();
            AddDateRangeParams(queryParams, from, to);

            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }

            return await GetAsync<GrossProfitReportModel>(url);
        }

        /// <summary>Item 154: all products with their current cost price.</summary>
        public async Task<List<ProductCostModel>> GetProductCostsAsync()
        {
            return await GetAsync<List<ProductCostModel>>("api/admin/reports/gross-profit/products");
        }

        /// <summary>Item 154: updates one product's cost price (giá vốn).</summary>
        public async Task<ProductCostModel> UpdateProductCostAsync(Guid id, decimal costPrice)
        {
            return await PutAsync<ProductCostModel>(
                $"api/admin/reports/gross-profit/products/{id}/cost-price",
                new { CostPrice = costPrice });
        }

        /// <summary>Wave 6c (item 33): admin sets/clears the internal note.</summary>
        public async Task<OrderModel> UpdateAdminNoteAsync(Guid orderId, string note)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/admin-note",
                new { Note = note });
        }

        /// <summary>
        /// Staff confirms a ShipperPickup order was collected at the shop.
        /// Optionally records the customer-booked shipper info.
        /// </summary>
        public async Task<OrderModel> MarkPickedUpAsync(Guid orderId, string shipperName, string shipperPhone)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/mark-picked-up",
                new { PickupShipperName = shipperName, PickupShipperPhone = shipperPhone });
        }

        /// <summary>Wave 6c (item 34): status-transition timeline, oldest first.</summary>
        public async Task<List<OrderStatusHistoryModel>> GetStatusHistoryAsync(Guid orderId)
        {
            return await GetAsync<List<OrderStatusHistoryModel>>($"api/orders/{orderId}/status-history");
        }

        /// <summary>
        /// Chuyển trạng thái đơn (vd Pending → Confirmed). Server tự validate
        /// luồng chuyển trạng thái hợp lệ — đổi sai quy trình sẽ bị server từ chối.
        /// Item 233: khi hủy đơn, truyền lý do hủy để báo cáo.
        /// </summary>
        /// <param name="orderId">Id đơn hàng.</param>
        /// <param name="status">Trạng thái mới.</param>
        /// <param name="cancelReason">Lý do hủy (chỉ dùng khi status = Cancelled).</param>
        /// <returns>Đơn hàng sau khi đổi trạng thái.</returns>
        public async Task<OrderModel> UpdateStatusAsync(
            Guid orderId, string status, string cancelReason = null)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/status",
                new UpdateOrderStatusModel { Status = status, CancelReason = cancelReason });
        }

        /// <summary>Admin confirms a bank-transfer payment.</summary>
        public async Task<OrderModel> ConfirmPaymentAsync(Guid orderId)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/confirm-payment", new { });
        }

        /// <summary>
        /// Item 233 (Admin): báo cáo số đơn hủy theo lý do trong khoảng thời gian.
        /// </summary>
        public async Task<List<CancelReasonReportModel>> GetCancelReasonReportAsync(
            DateTime? from, DateTime? to)
        {
            var url = "api/orders/cancel-reason-report";
            // Danh sách tham số query string (from/to) để gắn vào URL gọi API.
            var queryParams = new List<string>();
            AddDateRangeParams(queryParams, from, to);
            if (queryParams.Count > 0)
            {
                url += "?" + string.Join("&", queryParams);
            }
            return await GetAsync<List<CancelReasonReportModel>>(url);
        }

        /// <summary>
        /// Item 234 (Admin): món có ≥3 đánh giá 1–2 sao trong 30 ngày qua.
        /// </summary>
        public async Task<List<PoorlyRatedDishModel>> GetPoorlyRatedDishesAsync()
        {
            return await GetAsync<List<PoorlyRatedDishModel>>("api/orders/poorly-rated-dishes");
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

        /// <summary>Item 149: tags for a customer phone.</summary>
        public async Task<List<PhuThuongStickyRice.Web.Components.Admin.Models.CustomerTagModel>> GetCustomerTagsAsync(string phone)
        {
            var url = $"api/admin/customer-care/tags?phone={Uri.EscapeDataString(phone ?? "")}";
            return await GetAsync<List<PhuThuongStickyRice.Web.Components.Admin.Models.CustomerTagModel>>(url);
        }

        /// <summary>Item 149: add a tag to a customer phone.</summary>
        public async Task<PhuThuongStickyRice.Web.Components.Admin.Models.CustomerTagModel> AddCustomerTagAsync(
            string phone, string tagName)
        {
            return await PostAsync<PhuThuongStickyRice.Web.Components.Admin.Models.CustomerTagModel>(
                "api/admin/customer-care/tags",
                new { CustomerPhone = phone, TagName = tagName });
        }

        /// <summary>Item 149: remove a customer tag.</summary>
        public async Task DeleteCustomerTagAsync(Guid tagId)
        {
            await DeleteAsync($"api/admin/customer-care/tags/{tagId}");
        }

        /// <summary>
        /// Item 151: customer list (.xlsx) — same filters as the admin
        /// customer page: searchTerm, tag, inactiveOnly.
        /// </summary>
        public string GetExportCustomersUrl(string searchTerm = null, string tag = null, bool inactiveOnly = false)        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                parts.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                parts.Add($"tag={Uri.EscapeDataString(tag)}");
            }

            if (inactiveOnly)
            {
                parts.Add("inactiveOnly=true");
            }

            var query = parts.Count == 0 ? "" : "?" + string.Join("&", parts);
            // Dùng ApiBaseAddress ở ApiClientBase thay vì lặp TrimEnd('/') (codequality-team 2026-10-20).
            return $"{ApiBaseAddress}/api/admin/customer-care/customers/export{query}";
        }

        /// <summary>
        /// Item 168: detailed purchase history of one customer by phone —
        /// totals, first/last order, top 3 products, 10 most recent orders,
        /// CRM tags.
        /// </summary>
        public async Task<Admin.Models.CustomerDetailModel> GetCustomerDetailAsync(string phone)
        {
            return await GetAsync<Admin.Models.CustomerDetailModel>(
                $"api/admin/customers/{Uri.EscapeDataString(phone)}/detail");
        }

        /// <summary>
        /// Item 169: revenue summary of one date range (order count, total
        /// revenue, average order value, top 5 products). The compare view
        /// calls this once per period.
        /// </summary>
        public async Task<Admin.Models.RevenueSummaryModel> GetRevenueSummaryAsync(
            DateTime? from, DateTime? to)
        {
            var parts = new List<string>();
            AddDateRangeParams(parts, from, to);

            var url = "api/admin/orders/revenue-summary";
            if (parts.Count > 0)
            {
                url += "?" + string.Join("&", parts);
            }

            return await GetAsync<Admin.Models.RevenueSummaryModel>(url);
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

        /// <summary>
        /// Item 142: groups of possibly-duplicate orders (same phone,
        /// last 30 minutes, still Pending/Confirmed) for the admin panel.
        /// </summary>
        public async Task<List<DuplicateOrderGroupModel>> GetDuplicateOrdersAsync()
        {
            return await GetAsync<List<DuplicateOrderGroupModel>>("api/admin/orders/duplicates")
                ?? new List<DuplicateOrderGroupModel>();
        }

        /// <summary>
        /// Item 142: merges the given orders into the primary order
        /// (their lines are added, they are cancelled with a note).
        /// Always explicit — never automatic.
        /// </summary>
        public async Task<OrderModel> MergeOrdersAsync(Guid primaryOrderId, List<Guid> orderIds)
        {
            return await PostAsync<OrderModel>("api/orders/merge",
                new { PrimaryOrderId = primaryOrderId, OrderIds = orderIds });
        }
    }
}
