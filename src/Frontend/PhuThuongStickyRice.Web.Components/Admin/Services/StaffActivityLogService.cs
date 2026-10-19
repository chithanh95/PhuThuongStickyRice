using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Một dòng nhật ký hoạt động của nhân viên: ai đã làm gì, khi nào.
    /// Phục vụ kiểm soát nội bộ và truy vết thay đổi dữ liệu.
    /// </summary>
    public class StaffActivityLogModel
    {
        /// <summary>Id của dòng log.</summary>
        public Guid Id { get; set; }

        /// <summary>Id nhân viên thực hiện; null khi là hành động hệ thống.</summary>
        public Guid? UserId { get; set; }

        /// <summary>Tên đăng nhập/tên hiển thị của nhân viên.</summary>
        public string UserName { get; set; }

        /// <summary>Mã hành động (vd "product.update") dùng để lọc.</summary>
        public string Action { get; set; }

        /// <summary>Nhãn hành động tiếng Việt hiển thị trên lưới.</summary>
        public string ActionLabel { get; set; }

        /// <summary>Chi tiết thay đổi (vd tên món cũ → mới).</summary>
        public string Detail { get; set; }

        /// <summary>Thời điểm ghi log.</summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Kết quả phân trang nhật ký hoạt động, kèm danh sách mã hành động
    /// distinct để dựng dropdown lọc trên UI.
    /// </summary>
    public class StaffActivityLogListModel
    {
        /// <summary>Tổng số dòng log khớp bộ lọc (để phân trang).</summary>
        public long TotalCount { get; set; }

        /// <summary>Các dòng log của trang hiện tại.</summary>
        public List<StaffActivityLogModel> Items { get; set; } = new List<StaffActivityLogModel>();

        /// <summary>Các mã hành động distinct trong kết quả, dựng dropdown lọc.</summary>
        public List<string> Actions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Item 156: staff activity log viewer client (Admin only).
    /// <para>
    /// Service gọi API nhật ký hoạt động của nhân viên cho trang
    /// /admin/nhat-ky ("ai đã làm gì, khi nào") — phục vụ kiểm soát
    /// nội bộ và truy vết thay đổi dữ liệu.
    /// </para>
    /// </summary>
    public class StaffActivityLogService : ApiClientBase
    {
        /// <summary>Khởi tạo client gọi API nhật ký nhân viên.</summary>
        /// <param name="httpClient">HttpClient đã cấu hình base address API.</param>
        /// <param name="tokenStore">Kho token (chỉ admin đăng nhập mới có quyền xem log).</param>
        public StaffActivityLogService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
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
        /// <returns>Kết quả phân trang kèm danh sách mã hành động để dựng dropdown lọc.</returns>
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
