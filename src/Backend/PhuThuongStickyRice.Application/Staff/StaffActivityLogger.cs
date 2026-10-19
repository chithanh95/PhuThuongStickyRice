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
        Task LogAsync(string action, string detail = null);

        /// <summary>
        /// Logs for an explicit user (e.g. right after login, when there is
        /// no authenticated CurrentUser yet).
        /// </summary>
        Task LogForUserAsync(Guid userId, string userName, string action, string detail = null);
    }

    public class StaffActivityLogger : ApplicationService, IStaffActivityLogger
    {
        private static readonly string[] StaffRoles = { "Admin", "Cashier", "Kitchen" };

        private readonly IRepository<StaffActivityLog, Guid> _logs;
        private readonly IRepository<UserRole, Guid> _userRoles;
        private readonly ILogger<StaffActivityLogger> _logger;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

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

        public async Task LogAsync(string action, string detail = null)
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
                _logger.LogWarning(ex, "StaffActivityLogger failed for action {Action}.", action);
            }
        }

        public async Task LogForUserAsync(Guid userId, string userName, string action, string detail = null)
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
                _logger.LogWarning(ex, "StaffActivityLogger failed for action {Action}.", action);
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
    }
}
