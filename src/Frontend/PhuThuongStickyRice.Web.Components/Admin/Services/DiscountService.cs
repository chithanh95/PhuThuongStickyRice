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

        public async Task<DiscountCodeModel> CreateDiscountCodeAsync(DiscountCodeModel model)
        {
            return await PostAsync<DiscountCodeModel>("api/discounts", model);
        }

        public async Task<DiscountCodeModel> UpdateDiscountCodeAsync(Guid id, DiscountCodeModel model)
        {
            return await PutAsync<DiscountCodeModel>($"api/discounts/{id}", model);
        }

        public async Task DeleteDiscountCodeAsync(Guid id)
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

        /// <summary>
        /// perf-hunt 2026-10-19: mã giảm tiền tốt nhất cho giỏ hàng trong 1
        /// round-trip — thay vòng lặp N lần ValidateAsync ở checkout.
        /// </summary>
        public async Task<BestDiscountResultModel> EvaluateBestAsync(decimal subtotal, string customerPhone = null)
        {
            return await PostAsync<BestDiscountResultModel>("api/discounts/best",
                new { Subtotal = subtotal, CustomerPhone = customerPhone });
        }
    }
}
