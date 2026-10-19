using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Admin quản lý gói "Ăn sáng cả tuần" (JWT).
    /// </summary>
    public class WeeklyPlanAdminService : ApiClientBase
    {
        public WeeklyPlanAdminService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        public async Task<List<WeeklyPlanModel>> GetListAsync()
        {
            return await GetAsync<List<WeeklyPlanModel>>("api/weekly-plans")
                ?? new List<WeeklyPlanModel>();
        }

        public async Task<WeeklyPlanModel> CreateAsync(DateTime weekStart, List<WeeklyPlanDayInput> days)
        {
            return await PostAsync<WeeklyPlanModel>("api/weekly-plans",
                new { WeekStart = weekStart, Days = days });
        }
    }

    public class WeeklyPlanDayInput
    {
        public string DayOfWeek { get; set; }

        public Guid ProductId { get; set; }
    }
}
