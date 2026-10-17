using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Staff
{
    public interface IStaffActivityLogAppService : IApplicationService
    {
        Task<StaffActivityLogListDto> GetListAsync(GetStaffActivityLogsInput input);
    }

    /// <summary>Item 156: staff activity log viewer (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    public class StaffActivityLogAppService : ApplicationService, IStaffActivityLogAppService
    {
        private readonly IRepository<StaffActivityLog, Guid> _logs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public StaffActivityLogAppService(
            IRepository<StaffActivityLog, Guid> logs,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _logs = logs;
            _asyncExecuter = asyncExecuter;
        }

        public virtual async Task<StaffActivityLogListDto> GetListAsync(GetStaffActivityLogsInput input)
        {
            input ??= new GetStaffActivityLogsInput();

            var queryable = await _logs.GetQueryableAsync();
            var query = queryable.AsQueryable();

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
                var to = input.To.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt < to);
            }

            var total = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(
                query.OrderByDescending(x => x.CreatedAt)
                    .Skip(Math.Max(0, input.SkipCount))
                    .Take(Math.Clamp(input.MaxResultCount, 1, 200)));

            var actionsQueryable = await _logs.GetQueryableAsync();
            var actions = await _asyncExecuter.ToListAsync(
                actionsQueryable.Select(x => x.Action).Distinct());

            return new StaffActivityLogListDto
            {
                TotalCount = total,
                Items = items.Select(x => new StaffActivityLogDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    UserName = x.UserName,
                    Action = x.Action,
                    ActionLabel = StaffActions.Label(x.Action),
                    Detail = x.Detail,
                    CreatedAt = x.CreatedAt,
                }).ToList(),
                Actions = actions.OrderBy(a => a).ToList(),
            };
        }
    }
}
