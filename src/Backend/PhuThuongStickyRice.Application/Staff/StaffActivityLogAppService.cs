using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Staff
{
    /// <summary>
    /// Xem nhật ký hoạt động của nhân viên (item 156, chỉ admin): ai đã làm
    /// gì (chốt đơn, hủy đơn, đổi trạng thái...) và lúc nào. Dùng để rà soát
    /// thao tác và đối chiếu khi có khiếu nại.
    /// </summary>
    public interface IStaffActivityLogAppService : IApplicationService
    {
        /// <summary>
        /// Admin: xem log hoạt động có phân trang, lọc theo nhân viên, loại
        /// thao tác và khoảng ngày. Kết quả trả kèm danh sách các loại thao
        /// tác đã có để vẽ bộ lọc trên UI.
        /// </summary>
        /// <param name="input">Bộ lọc: UserId, Action, From, To, SkipCount, MaxResultCount.</param>
        /// <returns>Danh sách log theo trang + tổng số + các loại thao tác.</returns>
        Task<StaffActivityLogListDto> GetListAsync(GetStaffActivityLogsInput input);
    }

    /// <summary>
    /// Xem nhật ký hoạt động của nhân viên (chỉ admin): phục vụ tra cứu và
    /// đối chiếu thao tác (ai làm gì, lúc nào) trong ngày/giai đoạn cụ thể.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class StaffActivityLogAppService : ApplicationService, IStaffActivityLogAppService
    {
        private readonly IRepository<StaffActivityLog, Guid> _logs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IMemoryCache _memoryCache;

        /// <summary>Cache key danh sách loại thao tác — số action hiếm khi đổi nên cache 5 phút.</summary>
        private const string DistinctActionsCacheKey = "staff-log:distinct-actions";
        /// <summary>TTL cache danh sách loại thao tác (phút).</summary>
        private const int DistinctActionsCacheMinutes = 5;

        /// <summary>
        /// Khởi tạo service tra cứu nhật ký hoạt động nhân viên (Admin).
        /// </summary>
        public StaffActivityLogAppService(
            IRepository<StaffActivityLog, Guid> logs,
            IAsyncQueryableExecuter asyncExecuter,
            IMemoryCache memoryCache)
        {
            _logs = logs;
            _asyncExecuter = asyncExecuter;
            _memoryCache = memoryCache;
        }

        public virtual async Task<StaffActivityLogListDto> GetListAsync(GetStaffActivityLogsInput input)
        {
            // Client có thể không truyền filter — thay vì báo lỗi, dùng bộ
            // lọc rỗng để trả về toàn bộ log gần nhất.
            input ??= new GetStaffActivityLogsInput();

            var queryable = await _logs.GetQueryableAsync();
            var query = ApplyFilters(queryable.AsQueryable(), input);

            var total = await _asyncExecuter.LongCountAsync(query);
            // Chặn số dòng/trang từ 1 đến 200: tránh request tham số MaxResultCount
            // lớn/âm kéo sập DB hoặc trả về trang rỗng vì SkipCount âm.
            var items = await _asyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.CreatedAt)
                    .Skip(Math.Max(0, input.SkipCount))
                    .Take(Math.Clamp(input.MaxResultCount, 1, 200)));

            var actions = await GetDistinctActionsAsync();

            return new StaffActivityLogListDto
            {
                TotalCount = total,
                Items = items.Select(MapToDto).ToList(),
                Actions = actions.OrderBy(a => a).ToList(),
            };
        }

        /// <summary>
        /// Áp dụng bộ lọc danh sách log: theo nhân viên (Guid.Empty coi như
        /// không lọc để UI gửi giá trị mặc định của dropdown mà không lỗi),
        /// loại thao tác, khoảng ngày (ngày kết thúc là biên độc quyền +1 ngày
        /// để tính cả log lúc 23:59).
        /// </summary>
        private static IQueryable<StaffActivityLog> ApplyFilters(
            IQueryable<StaffActivityLog> query, GetStaffActivityLogsInput input)
        {
            // Guid.Empty coi như "không lọc theo người" để UI gửi giá trị
            // mặc định của dropdown nhân viên mà không lỗi.
            if (input.UserId.HasValue && input.UserId.Value != Guid.Empty)
            {
                query = query.Where(x => x.UserId == input.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.Action))
            {
                query = query.Where(x => x.Action == input.Action);
            }

            if (input.From.HasValue)
            {
                var from = input.From.Value.Date;
                query = query.Where(x => x.CreatedAt >= from);
            }

            if (input.To.HasValue)
            {
                // Ngày kết thúc là biên độc quyền (+1 ngày) để log trong cả
                // ngày To đều được tính, kể cả log phát sinh lúc 23:59.
                var to = input.To.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < to);
            }

            return query;
        }

        /// <summary>
        /// Lấy danh sách loại thao tác đã tồn tại trong bảng để UI vẽ dropdown
        /// bộ lọc, thay vì hardcode enum ở frontend (thêm action mới ở backend
        /// thì filter tự hiện).
        /// </summary>
        private async Task<List<string>> GetDistinctActionsAsync()
        {
            // perf-hunt 2026-10-19: trước đây mỗi request phân trang đều chạy
            // SELECT DISTINCT Action quét toàn bộ bảng log (tăng vô hạn theo
            // thời gian). Danh sách action hiếm khi đổi nên cache 5 phút —
            // action mới tạo tối đa 5 phút sau mới hiện trong dropdown lọc.
            if (_memoryCache.TryGetValue(DistinctActionsCacheKey, out List<string> cached))
            {
                return cached;
            }

            var actionsQueryable = await _logs.GetQueryableAsync();
            var actions = await _asyncExecuter.ToListAsync(
                actionsQueryable.Select(x => x.Action).Distinct());
            _memoryCache.Set(DistinctActionsCacheKey, actions,
                TimeSpan.FromMinutes(DistinctActionsCacheMinutes));
            return actions;
        }

        private static StaffActivityLogDto MapToDto(StaffActivityLog x) => new()
        {
            Id = x.Id,
            UserId = x.UserId,
            UserName = x.UserName,
            Action = x.Action,
            ActionLabel = StaffActivityActions.Label(x.Action),
            Detail = x.Detail,
            CreatedAt = x.CreatedAt,
        };
    }
}
