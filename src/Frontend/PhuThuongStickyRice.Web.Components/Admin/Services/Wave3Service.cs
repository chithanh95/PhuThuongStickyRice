using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Wave 3 admin APIs: shippers, blacklist, customer notes, promotions,
    /// reward rules, articles, table QRs, profit report, login audit and 2FA.
    /// </summary>
    public class Wave3Service : HttpService
    {
        public Wave3Service(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        // ---- Shippers (item 82) ----

        public async Task<List<ShipperModel>> GetShippersAsync(bool activeOnly = false)
        {
            return await GetAsync<List<ShipperModel>>($"api/shippers?activeOnly={activeOnly.ToString().ToLowerInvariant()}");
        }

        public async Task<ShipperModel> CreateShipperAsync(string name, string phone)
        {
            return await PostAsync<ShipperModel>("api/shippers", new { Name = name, Phone = phone });
        }

        public async Task<ShipperModel> UpdateShipperAsync(Guid id, string name, string phone, bool isActive)
        {
            return await PutAsync<ShipperModel>($"api/shippers/{id}",
                new { Name = name, Phone = phone, IsActive = isActive });
        }

        public async Task DeleteShipperAsync(Guid id)
        {
            await DeleteAsync($"api/shippers/{id}");
        }

        public async Task<OrderModel> AssignShipperAsync(Guid orderId, Guid? shipperId)
        {
            return await PutAsync<OrderModel>($"api/orders/{orderId}/assign-shipper",
                new { ShipperId = shipperId });
        }

        // ---- Kitchen display (item 81) ----

        public async Task<List<OrderModel>> GetKitchenOrdersAsync()
        {
            return await GetAsync<List<OrderModel>>("api/orders/kitchen");
        }

        public async Task<OrderModel> AdvanceKitchenStatusAsync(Guid orderId)
        {
            return await PostAsync<OrderModel>($"api/orders/{orderId}/kitchen/advance", new { });
        }

        // ---- Blacklist (item 88) ----

        public async Task<List<BlacklistedPhoneModel>> GetBlacklistAsync()
        {
            return await GetAsync<List<BlacklistedPhoneModel>>("api/blacklist");
        }

        public async Task<BlacklistedPhoneModel> AddToBlacklistAsync(string phone, string reason)
        {
            return await PostAsync<BlacklistedPhoneModel>("api/blacklist", new { Phone = phone, Reason = reason });
        }

        public async Task RemoveFromBlacklistAsync(Guid id)
        {
            await DeleteAsync($"api/blacklist/{id}");
        }

        // ---- Customer notes (item 89) ----

        public async Task<List<CustomerNoteModel>> GetCustomerNotesAsync(string phone)
        {
            return await GetAsync<List<CustomerNoteModel>>(
                $"api/customernotes?phone={Uri.EscapeDataString(phone ?? string.Empty)}");
        }

        public async Task<CustomerNoteModel> AddCustomerNoteAsync(string phone, string note)
        {
            return await PostAsync<CustomerNoteModel>("api/customernotes",
                new { Phone = phone, Note = note });
        }

        public async Task DeleteCustomerNoteAsync(Guid id)
        {
            await DeleteAsync($"api/customernotes/{id}");
        }

        // ---- Promotions (item 91) ----

        public async Task<List<PromotionModel>> GetPromotionsAsync()
        {
            return await GetAsync<List<PromotionModel>>("api/promotions");
        }

        /// <summary>Item 91: best currently-active promotion (public endpoint).</summary>
        public async Task<PromotionModel> GetActivePromotionAsync()
        {
            return await GetAsync<PromotionModel>("api/promotions/active");
        }

        public async Task<PromotionModel> CreatePromotionAsync(PromotionModel model)
        {
            return await PostAsync<PromotionModel>("api/promotions", model);
        }

        public async Task<PromotionModel> UpdatePromotionAsync(Guid id, PromotionModel model)
        {
            return await PutAsync<PromotionModel>($"api/promotions/{id}", model);
        }

        public async Task DeletePromotionAsync(Guid id)
        {
            await DeleteAsync($"api/promotions/{id}");
        }

        // ---- Reward rules (item 92) ----

        public async Task<List<OrderRewardRuleModel>> GetRewardRulesAsync()
        {
            return await GetAsync<List<OrderRewardRuleModel>>("api/rewardrules");
        }

        public async Task<OrderRewardRuleModel> CreateRewardRuleAsync(OrderRewardRuleModel model)
        {
            return await PostAsync<OrderRewardRuleModel>("api/rewardrules", model);
        }

        public async Task<OrderRewardRuleModel> UpdateRewardRuleAsync(Guid id, OrderRewardRuleModel model)
        {
            return await PutAsync<OrderRewardRuleModel>($"api/rewardrules/{id}", model);
        }

        public async Task DeleteRewardRuleAsync(Guid id)
        {
            await DeleteAsync($"api/rewardrules/{id}");
        }

        // ---- Articles (item 93) ----

        public async Task<List<ArticleModel>> GetArticlesAsync()
        {
            return await GetAsync<List<ArticleModel>>("api/articles");
        }

        public async Task<ArticleModel> CreateArticleAsync(ArticleModel model)
        {
            return await PostAsync<ArticleModel>("api/articles", model);
        }

        public async Task<ArticleModel> UpdateArticleAsync(Guid id, ArticleModel model)
        {
            return await PutAsync<ArticleModel>($"api/articles/{id}", model);
        }

        public async Task DeleteArticleAsync(Guid id)
        {
            await DeleteAsync($"api/articles/{id}");
        }

        // ---- Table QR (item 95) ----

        public async Task<List<TableQrModel>> GetTableQrsAsync()
        {
            return await GetAsync<List<TableQrModel>>("api/tableqrs");
        }

        public async Task<TableQrGeneratedModel> GenerateTableQrAsync(string label)
        {
            return await PostAsync<TableQrGeneratedModel>("api/tableqrs", new { Label = label });
        }

        public async Task DeleteTableQrAsync(Guid id)
        {
            await DeleteAsync($"api/tableqrs/{id}");
        }

        public string TableQrPngUrl(Guid id)
        {
            var baseUri = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
            return $"{baseUri}/api/tableqrs/{id:D}/png";
        }

        // ---- Profit report (item 84) ----

        public async Task<ProfitReportModel> GetProfitReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var url = "api/profitreport";
            var sep = "?";
            if (from.HasValue)
            {
                url += $"{sep}from={from.Value:yyyy-MM-dd}";
                sep = "&";
            }

            if (to.HasValue)
            {
                url += $"{sep}to={to.Value:yyyy-MM-dd}";
            }

            return await GetAsync<ProfitReportModel>(url);
        }

        // ---- Login audit (item 99) ----

        public async Task<PagedLoginAuditModel> GetLoginAuditsAsync(int page = 1, int pageSize = 50)
        {
            return await GetAsync<PagedLoginAuditModel>($"api/loginaudits?page={page}&pageSize={pageSize}");
        }

        // ---- 2FA (item 98) ----

        public async Task<TwoFactorSetupModel> GetTwoFactorSetupAsync()
        {
            return await GetAsync<TwoFactorSetupModel>("api/auth/2fa/setup");
        }

        public async Task EnableTwoFactorAsync(string secret, string code)
        {
            await PostAsync<object>("api/auth/2fa/enable", new { Secret = secret, Code = code });
        }

        public async Task DisableTwoFactorAsync(string code)
        {
            await PostAsync<object>("api/auth/2fa/disable", new { Code = code });
        }
    }
}
