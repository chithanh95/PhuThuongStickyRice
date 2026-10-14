using PhuThuongStickyRice.Application.AuditLogs.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.AuditLogs
{
    public interface IAuditLogAppService : IApplicationService
    {
        Task<PagedResultDto<AuditLogEntryDto>> GetListAsync(string searchTerm, int skipCount, int maxResultCount);
    }

    [Authorize(Roles = "Admin")]
    public class AuditLogAppService : ApplicationService, IAuditLogAppService
    {
        private readonly IRepository<AuditLogEntry, Guid> _auditLogs;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public AuditLogAppService(
            IRepository<AuditLogEntry, Guid> auditLogs,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _auditLogs = auditLogs;
            _asyncExecuter = asyncExecuter;
        }

        public async Task<PagedResultDto<AuditLogEntryDto>> GetListAsync(
            string searchTerm, int skipCount, int maxResultCount)
        {
            var queryable = await _auditLogs.GetQueryableAsync();
            System.Linq.IQueryable<AuditLogEntry> query = queryable.OrderByDescending(x => x.CreatedDateTime);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(x =>
                    x.Action.Contains(term) || x.ObjectId.Contains(term));
            }

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(
                query.Skip(Math.Max(0, skipCount)).Take(Math.Clamp(maxResultCount, 1, 100)));

            return new PagedResultDto<AuditLogEntryDto>(
                totalCount,
                items.Select(x => new AuditLogEntryDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Action = x.Action,
                    ObjectId = x.ObjectId,
                    Log = x.Log,
                    CreatedDateTime = x.CreatedDateTime
                }).ToList());
        }
    }
}
