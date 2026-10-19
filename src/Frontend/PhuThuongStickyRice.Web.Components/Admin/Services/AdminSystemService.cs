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
        public AdminSystemService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        // ---- Feature flags (item 105) ----

        public async Task<Dictionary<string, bool>> GetFeatureFlagMapAsync()
        {
            return await GetAsync<Dictionary<string, bool>>("api/featureflags");
        }

        public async Task<List<FeatureFlagModel>> GetFeatureFlagsAsync()
        {
            return await GetAsync<List<FeatureFlagModel>>("api/featureflags/admin");
        }

        public async Task<FeatureFlagModel> SetFeatureFlagAsync(string key, bool enabled)
        {
            return await PutAsync<FeatureFlagModel>($"api/featureflags/{Uri.EscapeDataString(key)}",
                new { Enabled = enabled });
        }

        // ---- Branches (item 109) ----

        public async Task<List<BranchModel>> GetActiveBranchesAsync()
        {
            return await GetAsync<List<BranchModel>>("api/branches");
        }

        public async Task<List<BranchModel>> GetBranchesAsync()
        {
            return await GetAsync<List<BranchModel>>("api/branches/admin");
        }

        public async Task<BranchModel> CreateBranchAsync(BranchModel model)
        {
            return await PostAsync<BranchModel>("api/branches/admin", model);
        }

        public async Task<BranchModel> UpdateBranchAsync(Guid id, BranchModel model)
        {
            return await PutAsync<BranchModel>($"api/branches/admin/{id}", model);
        }

        public async Task DeleteBranchAsync(Guid id)
        {
            await DeleteAsync($"api/branches/admin/{id}");
        }

        // ---- Webhooks (item 110) ----

        public async Task<List<WebhookEndpointModel>> GetWebhooksAsync()
        {
            return await GetAsync<List<WebhookEndpointModel>>("api/webhooks");
        }

        public async Task<WebhookEndpointModel> CreateWebhookAsync(WebhookEndpointModel model)
        {
            return await PostAsync<WebhookEndpointModel>("api/webhooks",
                new { model.Name, model.Url, model.Secret, Events = model.EventsCsv, model.IsActive });
        }

        public async Task<WebhookEndpointModel> UpdateWebhookAsync(Guid id, WebhookEndpointModel model)
        {
            return await PutAsync<WebhookEndpointModel>($"api/webhooks/{id}",
                new { model.Name, model.Url, model.Secret, Events = model.EventsCsv, model.IsActive });
        }

        public async Task DeleteWebhookAsync(Guid id)
        {
            await DeleteAsync($"api/webhooks/{id}");
        }

        public async Task<WebhookDeliveryModel> TestWebhookAsync(Guid id)
        {
            return await PostAsync<WebhookDeliveryModel>($"api/webhooks/{id}/test", new { });
        }

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

        public async Task<List<ShortLinkModel>> GetShortLinksAsync()
        {
            return await GetAsync<List<ShortLinkModel>>("api/shortlinks");
        }

        public async Task<ShortLinkModel> CreateShortLinkAsync(string targetUrl)
        {
            return await PostAsync<ShortLinkModel>("api/shortlinks", new { TargetUrl = targetUrl });
        }

        public async Task DeleteShortLinkAsync(Guid id)
        {
            await DeleteAsync($"api/shortlinks/{id}");
        }

        // ---- App logs (item 104) ----

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

        public async Task<DiskUsageModel> GetDiskUsageAsync()
        {
            return await GetAsync<DiskUsageModel>("api/system/disk-usage");
        }

        // ---- System status (item 111) ----

        public async Task<SystemStatusModel> GetSystemStatusAsync()
        {
            return await GetAsync<SystemStatusModel>("api/system/status");
        }

        // ---- Group orders (item 117) ----

        public async Task<GroupOrderModel> CreateGroupOrderAsync(string creatorPhone)
        {
            return await CreateGroupOrderAsync(creatorPhone, null, null);
        }

        public async Task<GroupOrderModel> CreateGroupOrderAsync(string creatorPhone, string groupName, DateTime? cutoffAt)
        {
            return await PostAsync<GroupOrderModel>("api/grouporders", new { CreatorPhone = creatorPhone, GroupName = groupName, CutoffAt = cutoffAt });
        }

        public async Task<GroupOrderModel> GetGroupOrderAsync(string code)
        {
            return await GetAsync<GroupOrderModel>($"api/grouporders/{Uri.EscapeDataString(code)}");
        }

        public async Task<GroupOrderModel> AddGroupOrderLineAsync(string code, Guid productId, int qty, string note, string addedBy)
        {
            return await PostAsync<GroupOrderModel>($"api/grouporders/{Uri.EscapeDataString(code)}/lines",
                new { ProductId = productId, Quantity = qty, Note = note, AddedBy = addedBy });
        }

        public async Task<CloseGroupOrderResultModel> CloseGroupOrderAsync(
            string code, string customerName, string customerPhone, string customerAddress, string notes)
        {
            return await PostAsync<CloseGroupOrderResultModel>(
                $"api/grouporders/{Uri.EscapeDataString(code)}/close",
                new { CustomerName = customerName, CustomerPhone = customerPhone, CustomerAddress = customerAddress, Notes = notes });
        }

        // ---- Address book (item 119) ----

        public async Task<List<AddressBookModel>> GetAddressesAsync(string phone)
        {
            return await GetAsync<List<AddressBookModel>>($"api/addressbooks?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        public async Task<AddressBookModel> SaveAddressAsync(string phone, string label, string address)
        {
            return await PostAsync<AddressBookModel>("api/addressbooks",
                new { Phone = phone, Label = label, Address = address });
        }

        public async Task DeleteAddressAsync(Guid id, string phone)
        {
            await DeleteAsync($"api/addressbooks/{id}?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }
    }
}
