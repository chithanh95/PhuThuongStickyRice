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
        /// <summary>
        /// Client gọi API dashboard của admin: thống kê tổng quan, món bán chạy,
        /// khách mới/cũ, doanh thu hôm nay so với hôm qua, báo cáo tuần và trạng thái Zalo OA.
        /// Các URL tự gắn tham số chống cache (_=timestamp) để luôn lấy số liệu mới nhất.
        /// </summary>
    public class DashboardService : ApiClientBase
    {
        /// <summary>Khởi tạo client dashboard (dùng HttpClient đã có token của admin).</summary>
        public DashboardService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Lấy số liệu tổng quan dashboard (doanh thu, số đơn, món bán...) trong khoảng ngày;
        /// bỏ trống from/to để dùng khoảng mặc định của server.
        /// </summary>
        /// <param name="from">Từ ngày (tùy chọn).</param>
        /// <param name="to">Đến ngày (tùy chọn).</param>
        /// <returns>Số liệu thống kê tổng quan.</returns>
        public async Task<DashboardStatsModel> GetStatsAsync(DateTime? from = null, DateTime? to = null)
        {
            return await GetAsync<DashboardStatsModel>(BuildUrl("api/dashboard/stats", from, to));
        }

        /// <summary>
        /// Lấy top N món bán chạy nhất trong khoảng ngày (mặc định 5).
        /// </summary>
        /// <param name="from">Từ ngày (tùy chọn).</param>
        /// <param name="to">Đến ngày (tùy chọn).</param>
        /// <param name="top">Số món lấy, mặc định 5.</param>
        /// <returns>Danh sách món bán chạy.</returns>
        public async Task<List<TopProductModel>> GetTopProductsAsync(DateTime? from = null, DateTime? to = null, int top = 5)
        {
            return await GetAsync<List<TopProductModel>>(BuildUrl("api/dashboard/top-products", from, to) + $"&top={top}");
        }

        /// <summary>
        /// Item 90: khách mới (đơn đầu tiên trong khoảng) so với khách quay
        /// lại (đã mua trước đó) trong khoảng ngày.
        /// </summary>
        /// <param name="from">Từ ngày (tùy chọn).</param>
        /// <param name="to">Đến ngày (tùy chọn).</param>
        /// <returns>Số lượng khách mới/cũ.</returns>
        public async Task<NewAndReturningCustomersModel> GetNewReturningCustomersAsync(DateTime? from = null, DateTime? to = null)
        {
            return await GetAsync<NewAndReturningCustomersModel>(
                BuildUrl("api/dashboard/customers/new-vs-returning", from, to));
        }

        /// <summary>
        /// Item 36 (wave 6d): so sánh doanh thu và số đơn hôm nay với hôm qua.
        /// </summary>
        /// <returns>Doanh thu/đơn hôm nay và hôm qua.</returns>
        public async Task<TodayVsYesterdayModel> GetTodayVsYesterdayAsync()
        {
            return await GetAsync<TodayVsYesterdayModel>(
                "api/dashboard/today-vs-yesterday?_=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        /// <summary>
        /// Item 138: số liệu tổng hợp của tuần đầy đủ gần nhất (doanh thu,
        /// số đơn, món bán chạy...) để gửi báo cáo.
        /// </summary>
        /// <returns>Báo cáo tuần.</returns>
        public async Task<WeeklyReportModel> GetWeeklyReportAsync()
        {
            return await GetAsync<WeeklyReportModel>(
                "api/admin/reports/weekly?_=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        /// <summary>
        /// Item 138: kích hoạt gửi báo cáo tuần qua Zalo OA cho admin.
        /// </summary>
        /// <returns>Kết quả gửi báo cáo.</returns>
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
