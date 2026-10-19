using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    public class DiscountService : HttpService
    {
        public DiscountService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<List<DiscountCodeModel>> GetListAsync()
        {
            return await GetAsync<List<DiscountCodeModel>>("api/discounts");
        }

        public async Task<DiscountCodeModel> CreateAsync(DiscountCodeModel model)
        {
            return await PostAsync<DiscountCodeModel>("api/discounts", model);
        }

        public async Task<DiscountCodeModel> UpdateAsync(Guid id, DiscountCodeModel model)
        {
            return await PutAsync<DiscountCodeModel>($"api/discounts/{id}", model);
        }

        public async Task DeleteAsync(Guid id)
        {
            await DeleteAsync($"api/discounts/{id}");
        }

        public async Task<ValidateDiscountResultModel> ValidateAsync(string code, decimal subtotal, string customerPhone = null)
        {
            return await PostAsync<ValidateDiscountResultModel>("api/discounts/validate",
                new { Code = code, Subtotal = subtotal, CustomerPhone = customerPhone });
        }

        /// <summary>Item 139: active public codes for checkout auto-apply.</summary>
        public async Task<List<DiscountCodeModel>> GetActivePublicAsync()
        {
            return await GetAsync<List<DiscountCodeModel>>("api/discounts/active");
        }
    }
}
