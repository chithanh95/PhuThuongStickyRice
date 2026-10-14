using PhuThuongStickyRice.Blazor.Modules.Admin.Models;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Admin.Services
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
    }
}
