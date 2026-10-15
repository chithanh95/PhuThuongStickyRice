using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Dashboard.Models;
using PhuThuongStickyRice.Web.Components.Orders.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Dashboard.Services
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

        /// <summary>Wave 6d (item 36): today's vs yesterday's revenue and orders.</summary>
        public async Task<TodayVsYesterdayModel> GetTodayVsYesterdayAsync()
        {
            return await GetAsync<TodayVsYesterdayModel>(
                "api/dashboard/today-vs-yesterday?_=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        /// <summary>Item 138: aggregated numbers for the last full week.</summary>
        public async Task<WeeklyReportModel> GetWeeklyReportAsync()
        {
            return await GetAsync<WeeklyReportModel>(
                "api/admin/reports/weekly?_=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        /// <summary>Item 138: trigger a Zalo OA delivery of the weekly report (Admin).</summary>
        public async Task<WeeklyReportSendResultModel> SendWeeklyReportAsync()
        {
            return await PostAsync<WeeklyReportSendResultModel>("api/admin/reports/weekly/send");
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
