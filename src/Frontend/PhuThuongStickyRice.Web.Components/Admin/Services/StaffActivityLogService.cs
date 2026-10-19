using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    public class StaffActivityLogModel
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string ActionLabel { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StaffActivityLogListModel
    {
        public long TotalCount { get; set; }
        public List<StaffActivityLogModel> Items { get; set; } = new List<StaffActivityLogModel>();
        public List<string> Actions { get; set; } = new List<string>();
    }

    /// <summary>Item 156: staff activity log viewer client (Admin only).</summary>
    /// <summary>
    /// Service gọi API nhật ký hoạt động của nhân viên cho trang
    /// /admin/nhat-ky ("ai đã làm gì, khi nào") — phục vụ kiểm soát
    /// nội bộ và truy vết thay đổi dữ liệu.
    /// </summary>
    public class StaffActivityLogService : HttpService
    {
        public StaffActivityLogService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        /// <summary>
        /// Lấy danh sách log có phân trang và lọc theo nhân viên / hành động / khoảng ngày.
        /// </summary>
        /// <param name="userId">Lọc theo nhân viên; null hoặc Guid.Empty = tất cả.</param>
        /// <param name="action">Lọc theo mã hành động; rỗng = tất cả.</param>
        /// <param name="from">Từ ngày (yyyy-MM-dd); null = không giới hạn dưới.</param>
        /// <param name="to">Đến ngày (yyyy-MM-dd); null = không giới hạn trên.</param>
        /// <param name="skipCount">Số bản ghi bỏ qua (phân trang).</param>
        /// <param name="maxResultCount">Số bản ghi mỗi trang, mặc định 50.</param>
        public Task<StaffActivityLogListModel> GetLogsAsync(
            Guid? userId, string action, DateTime? from, DateTime? to, int skipCount, int maxResultCount = 50)
        {
            var query = $"api/admin/staff-logs?skipCount={skipCount}&maxResultCount={maxResultCount}";
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                query += $"&userId={userId.Value:D}";
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query += $"&action={Uri.EscapeDataString(action)}";
            }

            if (from.HasValue)
            {
                query += $"&from={from.Value:yyyy-MM-dd}";
            }

            if (to.HasValue)
            {
                query += $"&to={to.Value:yyyy-MM-dd}";
            }

            return GetAsync<StaffActivityLogListModel>(query);
        }
    }
}
