using PhuThuongStickyRice.Blazor.Modules.Admin.Models;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Orders.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Dashboard.Services
{
    public class DashboardService : HttpService
    {
        public DashboardService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<DashboardStatsModel> GetStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            return await GetAsync<DashboardStatsModel>(BuildUrl("api/dashboard/stats", from, to));
        }

        public async Task<List<TopProductModel>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5)
        {
            return await GetAsync<List<TopProductModel>>(BuildUrl("api/dashboard/top-products", from, to) + $"&top={top}");
        }

        /// <summary>Item 90: new vs returning customers over the date range.</summary>
        public async Task<NewReturningCustomersModel> GetNewReturningCustomersAsync(DateTime? from = null, DateTime? to = null)
        {
            return await GetAsync<NewReturningCustomersModel>(
                BuildUrl("api/dashboard/customers/new-vs-returning", from, to));
        }

        private static string BuildUrl(string baseUrl, DateTime? from, DateTime? to)
        {
            var url = baseUrl + "?_=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (from.HasValue)
            {
                url += $"&from={from.Value:yyyy-MM-dd}";
            }

            if (to.HasValue)
            {
                url += $"&to={to.Value:yyyy-MM-dd}";
            }

            return url;
        }
    }
}
