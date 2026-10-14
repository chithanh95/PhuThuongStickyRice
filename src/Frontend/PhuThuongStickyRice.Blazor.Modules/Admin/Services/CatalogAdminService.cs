using PhuThuongStickyRice.Blazor.Modules.Admin.Models;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Admin.Services
{
    /// <summary>
    /// Admin CRUD for the Wave 1 catalog features: toppings, combos, quote
    /// requests, dish candidates and product size options.
    /// </summary>
    public class CatalogAdminService : HttpService
    {
        public CatalogAdminService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        // Toppings.

        public async Task<List<ToppingModel>> GetToppingsAsync()
        {
            return await GetAsync<List<ToppingModel>>("api/toppings");
        }

        public async Task<ToppingModel> CreateToppingAsync(ToppingModel model)
        {
            return await PostAsync<ToppingModel>("api/toppings", model);
        }

        public async Task<ToppingModel> UpdateToppingAsync(Guid id, ToppingModel model)
        {
            return await PutAsync<ToppingModel>($"api/toppings/{id}", model);
        }

        public async Task DeleteToppingAsync(Guid id)
        {
            await DeleteAsync($"api/toppings/{id}");
        }

        // Combos.

        public async Task<List<ComboModel>> GetCombosAsync()
        {
            return await GetAsync<List<ComboModel>>("api/combos");
        }

        public async Task<ComboModel> CreateComboAsync(ComboModel model)
        {
            return await PostAsync<ComboModel>("api/combos", model);
        }

        public async Task<ComboModel> UpdateComboAsync(Guid id, ComboModel model)
        {
            return await PutAsync<ComboModel>($"api/combos/{id}", model);
        }

        public async Task DeleteComboAsync(Guid id)
        {
            await DeleteAsync($"api/combos/{id}");
        }

        // Quote requests.

        public async Task<List<QuoteRequestAdminModel>> GetQuotesAsync(string status = null)
        {
            var url = "api/quotes?page=1&pageSize=100"
                + (string.IsNullOrWhiteSpace(status) ? "" : $"&status={Uri.EscapeDataString(status)}");
            var paged = await GetAsync<PagedResult<QuoteRequestAdminModel>>(url);
            return paged?.Items ?? new List<QuoteRequestAdminModel>();
        }

        public async Task<QuoteRequestAdminModel> UpdateQuoteStatusAsync(Guid id, string status)
        {
            return await PutAsync<QuoteRequestAdminModel>($"api/quotes/{id}/status", new { Status = status });
        }

        // Dish candidates.

        public async Task<List<DishCandidateModel>> GetDishCandidatesAsync()
        {
            return await GetAsync<List<DishCandidateModel>>("api/dish-candidates/all");
        }

        public async Task<DishCandidateModel> CreateDishCandidateAsync(DishCandidateModel model)
        {
            return await PostAsync<DishCandidateModel>("api/dish-candidates/manage", model);
        }

        public async Task<DishCandidateModel> UpdateDishCandidateAsync(Guid id, DishCandidateModel model)
        {
            return await PutAsync<DishCandidateModel>($"api/dish-candidates/manage/{id}", model);
        }

        public async Task DeleteDishCandidateAsync(Guid id)
        {
            await DeleteAsync($"api/dish-candidates/manage/{id}");
        }

        // Product size options.

        public async Task<List<ProductSizeOptionModel>> GetSizeOptionsAsync(Guid productId)
        {
            return await GetAsync<List<ProductSizeOptionModel>>($"api/product-size-options/by-product/{productId}");
        }

        public async Task<ProductSizeOptionModel> CreateSizeOptionAsync(ProductSizeOptionModel model)
        {
            return await PostAsync<ProductSizeOptionModel>("api/product-size-options", model);
        }

        public async Task<ProductSizeOptionModel> UpdateSizeOptionAsync(Guid id, ProductSizeOptionModel model)
        {
            return await PutAsync<ProductSizeOptionModel>($"api/product-size-options/{id}", model);
        }

        public async Task DeleteSizeOptionAsync(Guid id)
        {
            await DeleteAsync($"api/product-size-options/{id}");
        }

        private sealed class PagedResult<T>
        {
            public long TotalCount { get; set; }

            public List<T> Items { get; set; } = new List<T>();
        }
    }
}
