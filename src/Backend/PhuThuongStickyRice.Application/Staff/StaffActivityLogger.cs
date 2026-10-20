using PhuThuongStickyRice.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Staff
{
    /// <summary>
    /// Item 156: writes <see cref="StaffActivityLog"/> rows for important
    /// staff actions. Logging must never break the business flow, so every
    /// failure is swallowed after being logged to the app logger. Only
    /// staff roles (Admin/Cashier/Kitchen) are recorded — customer actions
    /// are not staff activity.
    /// </summary>
    public interface IStaffActivityLogger : IApplicationService
    {
        /// <summary>
        /// Ghi log hành động của nhân viên đang đăng nhập. Best-effort: mọi
        /// lỗi ghi log đều được nuốt sau khi log, không bao giờ làm gãy luồng
        /// nghiệp vụ. Hành động của khách không phải nhân viên bị bỏ qua.
        /// </summary>
        /// <param name="action">Tên hành động (vd "Tạo đơn", "Sửa giá").</param>
        /// <param name="detail">Chi tiết bổ sung.</param>
        Task LogAsync(string action, string detail = null);

        /// <summary>
        /// Logs for an explicit user (e.g. right after login, when there is
        /// no authenticated CurrentUser yet).
        /// </summary>
        Task LogForUserAsync(Guid userId, string userName, string action, string detail = null);
    }

    /// <summary>
    /// Item 156: triển khai ghi log hoạt động của nhân viên — chỉ ghi hành
    /// động của các role Admin/Cashier/Kitchen; lỗi khi ghi log không bao
    /// giờ làm gãy luồng nghiệp vụ. Xem hợp đồng chi tiết ở
    /// <see cref="IStaffActivityLogger"/>.
    /// </summary>
    public partial class StaffActivityLogger : ApplicationService, IStaffActivityLogger
    {
        private static readonly string[] StaffRoles = { "Admin", "Cashier", "Kitchen" };

        private readonly IRepository<StaffActivityLog, Guid> _logs;
        private readonly IRepository<UserRole, Guid> _userRoles;
        private readonly ILogger<StaffActivityLogger> _logger;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>
        /// Khởi tạo logger hoạt động nhân viên: ghi <see cref="StaffActivityLog"/>
        /// cho các role nhân viên, lỗi ghi log không làm gãy luồng chính.
        /// </summary>
        public StaffActivityLogger(
            IRepository<StaffActivityLog, Guid> logs,
            IRepository<UserRole, Guid> userRoles,
            ILogger<StaffActivityLogger> logger,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _logs = logs;
            _userRoles = userRoles;
            _logger = logger;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Ghi log cho nhân viên đang đăng nhập: bỏ qua khi chưa login hoặc
        /// không phải role nhân viên (Admin/Cashier/Kitchen). Dùng
        /// autoSave vì nơi gọi gồm cả controller không có ambient UOW.
        /// </summary>
        public virtual async Task LogAsync(string action, string detail = null)
        {
            try
            {
                if (CurrentUser?.Id == null || !CurrentUser.IsAuthenticated)
                {
                    return;
                }

                if (!await IsStaffAsync(CurrentUser.Id.Value))
                {
                    return;
                }

                var entry = new StaffActivityLog
                {
                    UserId = CurrentUser.Id,
                    UserName = CurrentUser.UserName ?? CurrentUser.Id.ToString(),
                    Action = action,
                    Detail = detail,
                    CreatedAt = DateTime.UtcNow,
                };

                // autoSave: callers include controllers with no ambient UOW.
                await _logs.InsertAsync(entry, autoSave: true);
            }
            catch (Exception ex)
            {
                // Activity logging is best-effort: never break the flow.
                LogActivityFailed(_logger, action, ex);
            }
        }

        /// <summary>
        /// Ghi log cho 1 user chỉ định (vd ngay sau login, lúc chưa có
        /// CurrentUser xác thực). Cũng best-effort như <see cref="LogAsync"/>.
        /// </summary>
        public virtual async Task LogForUserAsync(Guid userId, string userName, string action, string detail = null)
        {
            try
            {
                // autoSave: typically called from a controller with no ambient UOW.
                await _logs.InsertAsync(new StaffActivityLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    Detail = detail,
                    CreatedAt = DateTime.UtcNow,
                }, autoSave: true);
            }
            catch (Exception ex)
            {
                LogActivityFailed(_logger, action, ex);
            }
        }

        /// <summary>
        /// perf-hunt 2026-10-19: kiểm tra role bằng EXISTS trên SQL
        /// (mảng StaffRoles thành IN clause) thay vì stream toàn bộ tên role
        /// về client rồi so chuỗi lồng nhau trong C# — 1 query có index thay
        /// vì O(n) streaming. So khớp không phân biệt hoa thường như cũ nhờ
        /// collation CI của database.
        /// </summary>
        private async Task<bool> IsStaffAsync(Guid userId)
        {
            var queryable = await _userRoles.GetQueryableAsync();
            return await _asyncExecuter.AnyAsync(
                queryable.Where(ur => ur.UserId == userId)
                    .Select(ur => ur.Role.Name)
                    .Where(n => StaffRoles.Contains(n)));
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated.

        /// <summary>Log khi ghi activity thất bại (best-effort, không ném lỗi).</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "StaffActivityLogger failed for action {Action}.")]
        private static partial void LogActivityFailed(ILogger logger, string action, Exception ex);
    }
}
