using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Admin system APIs: feature flags, branches, webhooks, short links,
    /// app logs, disk usage, system status, group orders, address book.
    /// Admin endpoints attach the JWT; public ones work anonymously.
    /// </summary>
    public class AdminSystemService : HttpService
    {
        /// <summary>Khởi tạo client gọi API hệ thống (admin).</summary>
        /// <param name="httpClient">HttpClient đã gắn base address API.</param>
        /// <param name="tokenStore">Quản lý JWT gắn vào header.</param>
        public AdminSystemService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        // ---- Feature flags (item 105) ----

        /// <summary>Lấy map cờ tính năng (key → bật/tắt) cho toàn bộ web.</summary>
        public async Task<Dictionary<string, bool>> GetFeatureFlagMapAsync()
        {
            return await GetAsync<Dictionary<string, bool>>("api/featureflags");
        }

        /// <summary>Lấy danh sách chi tiết cờ tính năng (admin).</summary>
        public async Task<List<FeatureFlagModel>> GetFeatureFlagsAsync()
        {
            return await GetAsync<List<FeatureFlagModel>>("api/featureflags/admin");
        }

        /// <summary>Bật/tắt một cờ tính năng.</summary>
        /// <param name="key">Khóa cờ (được escape vì có thể chứa ký tự đặc biệt).</param>
        /// <param name="enabled">true = bật, false = tắt.</param>
        public async Task<FeatureFlagModel> SetFeatureFlagAsync(string key, bool enabled)
        {
            return await PutAsync<FeatureFlagModel>($"api/featureflags/{Uri.EscapeDataString(key)}",
                new { Enabled = enabled });
        }

        // ---- Branches (item 109) ----

        /// <summary>Lấy danh sách chi nhánh đang hoạt động (public).</summary>
        public async Task<List<BranchModel>> GetActiveBranchesAsync()
        {
            return await GetAsync<List<BranchModel>>("api/branches");
        }

        /// <summary>Lấy toàn bộ chi nhánh (kể cả đã tắt, admin).</summary>
        public async Task<List<BranchModel>> GetBranchesAsync()
        {
            return await GetAsync<List<BranchModel>>("api/branches/admin");
        }

        /// <summary>Tạo chi nhánh mới.</summary>
        /// <param name="model">Thông tin chi nhánh.</param>
        public async Task<BranchModel> CreateBranchAsync(BranchModel model)
        {
            return await PostAsync<BranchModel>("api/branches/admin", model);
        }

        /// <summary>Cập nhật chi nhánh.</summary>
        /// <param name="id">Id chi nhánh.</param>
        /// <param name="model">Thông tin chi nhánh mới.</param>
        public async Task<BranchModel> UpdateBranchAsync(Guid id, BranchModel model)
        {
            return await PutAsync<BranchModel>($"api/branches/admin/{id}", model);
        }

        /// <summary>Xóa chi nhánh.</summary>
        /// <param name="id">Id chi nhánh.</param>
        public async Task DeleteBranchAsync(Guid id)
        {
            await DeleteAsync($"api/branches/admin/{id}");
        }

        // ---- Webhooks (item 110) ----

        /// <summary>Lấy danh sách endpoint webhook.</summary>
        public async Task<List<WebhookEndpointModel>> GetWebhooksAsync()
        {
            return await GetAsync<List<WebhookEndpointModel>>("api/webhooks");
        }

        /// <summary>
        /// Tạo endpoint webhook mới. Danh sách sự kiện gửi đi dưới dạng chuỗi
        /// <c>EventsCsv</c> (ngăn cách bằng dấu phẩy).
        /// </summary>
        /// <param name="model">Tên, URL, secret và sự kiện đăng ký.</param>
        public async Task<WebhookEndpointModel> CreateWebhookAsync(WebhookEndpointModel model)
        {
            return await PostAsync<WebhookEndpointModel>("api/webhooks",
                new { model.Name, model.Url, model.Secret, Events = model.EventsCsv, model.IsActive });
        }

        /// <summary>Cập nhật endpoint webhook.</summary>
        /// <param name="id">Id endpoint.</param>
        /// <param name="model">Thông tin endpoint mới.</param>
        public async Task<WebhookEndpointModel> UpdateWebhookAsync(Guid id, WebhookEndpointModel model)
        {
            return await PutAsync<WebhookEndpointModel>($"api/webhooks/{id}",
                new { model.Name, model.Url, model.Secret, Events = model.EventsCsv, model.IsActive });
        }

        /// <summary>Xóa endpoint webhook.</summary>
        /// <param name="id">Id endpoint.</param>
        public async Task DeleteWebhookAsync(Guid id)
        {
            await DeleteAsync($"api/webhooks/{id}");
        }

        /// <summary>Gửi thử một webhook tới endpoint (ping kiểm tra URL/secret).</summary>
        /// <param name="id">Id endpoint cần test.</param>
        public async Task<WebhookDeliveryModel> TestWebhookAsync(Guid id)
        {
            return await PostAsync<WebhookDeliveryModel>($"api/webhooks/{id}/test", new { });
        }

        /// <summary>Lấy lịch sử gửi webhook (toàn bộ hoặc theo 1 endpoint).</summary>
        /// <param name="endpointId">Id endpoint; null = tất cả endpoint.</param>
        public async Task<List<WebhookDeliveryModel>> GetWebhookDeliveriesAsync(Guid? endpointId = null)
        {
            var url = "api/webhooks/deliveries";
            if (endpointId.HasValue)
            {
                url += $"?endpointId={endpointId.Value}";
            }

            return await GetAsync<List<WebhookDeliveryModel>>(url);
        }

        // ---- Short links (item 116) ----

        /// <summary>Lấy danh sách link rút gọn.</summary>
        public async Task<List<ShortLinkModel>> GetShortLinksAsync()
        {
            return await GetAsync<List<ShortLinkModel>>("api/shortlinks");
        }

        /// <summary>Tạo link rút gọn mới cho một URL đích.</summary>
        /// <param name="targetUrl">URL đích cần rút gọn.</param>
        public async Task<ShortLinkModel> CreateShortLinkAsync(string targetUrl)
        {
            return await PostAsync<ShortLinkModel>("api/shortlinks", new { TargetUrl = targetUrl });
        }

        /// <summary>Xóa link rút gọn.</summary>
        /// <param name="id">Id link rút gọn.</param>
        public async Task DeleteShortLinkAsync(Guid id)
        {
            await DeleteAsync($"api/shortlinks/{id}");
        }

        // ---- App logs (item 104) ----

        /// <summary>
        /// Lấy nhật ký ứng dụng có phân trang; chỉ gắn query param level/searchTerm
        /// khi có giá trị để backend không lọc thừa.
        /// </summary>
        /// <param name="level">Mức log (VD: Error, Warning); null = tất cả.</param>
        /// <param name="searchTerm">Từ khóa tìm kiếm; null = không lọc.</param>
        /// <param name="page">Trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang.</param>
        public async Task<PagedAppLogsModel> GetAppLogsAsync(string level, string searchTerm, int page, int pageSize)
        {
            var url = $"api/applogs?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(level))
            {
                url += $"&level={Uri.EscapeDataString(level)}";
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<PagedAppLogsModel>(url);
        }

        // ---- Disk usage (item 113) ----

        /// <summary>Lấy thông tin dung lượng ổ đĩa server (giám sát hệ thống).</summary>
        public async Task<DiskUsageModel> GetDiskUsageAsync()
        {
            return await GetAsync<DiskUsageModel>("api/system/disk-usage");
        }

        // ---- System status (item 111) ----

        /// <summary>Lấy trạng thái hệ thống (sức khỏe dịch vụ, uptime...).</summary>
        public async Task<SystemStatusModel> GetSystemStatusAsync()
        {
            return await GetAsync<SystemStatusModel>("api/system/status");
        }

        // ---- Address book (item 119) ----

        /// <summary>Lấy sổ địa chỉ đã lưu của khách theo SĐT.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        public async Task<List<AddressBookModel>> GetAddressesAsync(string phone)
        {
            return await GetAsync<List<AddressBookModel>>($"api/addressbooks?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        /// <summary>Lưu địa chỉ mới vào sổ địa chỉ của khách.</summary>
        /// <param name="phone">SĐT khách hàng.</param>
        /// <param name="label">Nhãn địa chỉ (VD: "Nhà", "Công ty").</param>
        /// <param name="address">Địa chỉ chi tiết.</param>
        public async Task<AddressBookModel> SaveAddressAsync(string phone, string label, string address)
        {
            return await PostAsync<AddressBookModel>("api/addressbooks",
                new { Phone = phone, Label = label, Address = address });
        }

        /// <summary>Xóa địa chỉ khỏi sổ địa chỉ của khách.</summary>
        /// <param name="id">Id địa chỉ.</param>
        /// <param name="phone">SĐT chủ sở hữu (để xác thực quyền xóa).</param>
        public async Task DeleteAddressAsync(Guid id, string phone)
        {
            await DeleteAsync($"api/addressbooks/{id}?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }
    }
}
