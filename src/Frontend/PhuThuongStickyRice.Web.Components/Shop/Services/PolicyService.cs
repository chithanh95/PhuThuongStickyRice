using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using PhuThuongStickyRice.CrossCuttingConcerns.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Shop.Services
{
    /// <summary>
    /// Wave 6b (item 28): policy pages — public list/by-slug, admin
    /// list/update. Follows the FAQ service pattern.
    /// </summary>
    public class PolicyService : HttpService
    {
        public PolicyService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        /// <summary>Public: slug + title of every page (footer links).</summary>
        public async Task<List<PolicyPageSummaryModel>> GetPublicListAsync()
        {
            return await GetAsync<List<PolicyPageSummaryModel>>("api/policies")
                ?? new List<PolicyPageSummaryModel>();
        }

        /// <summary>Public: one page by slug; null when the slug is unknown.</summary>
        public async Task<PolicyPageModel> GetBySlugAsync(string slug)
        {
            await SetBearerToken();
            var response = await _httpClient.GetAsync(
                $"api/policies/{Uri.EscapeDataString(slug ?? string.Empty)}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAs<PolicyPageModel>();
        }

        /// <summary>Admin: all pages with bodies.</summary>
        public async Task<List<PolicyPageModel>> GetAllAsync()
        {
            return await GetAsync<List<PolicyPageModel>>("api/policies/all")
                ?? new List<PolicyPageModel>();
        }

        /// <summary>Admin: update title/body/order.</summary>
        public async Task<PolicyPageModel> UpdateAsync(Guid id, PolicyPageModel model)
        {
            return await PutAsync<PolicyPageModel>($"api/policies/{id}", new
            {
                Title = model.Title,
                Body = model.Body,
                SortOrder = model.SortOrder
            });
        }
    }
}
