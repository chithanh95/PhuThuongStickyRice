using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Orders.Models;
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

        public async Task<DashboardStatsModel> GetStatsAsync()
        {
            return await GetAsync<DashboardStatsModel>("api/dashboard/stats");
        }
    }
}
