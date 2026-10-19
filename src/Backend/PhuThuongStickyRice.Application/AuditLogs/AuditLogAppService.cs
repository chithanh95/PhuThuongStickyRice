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
    /// <summary>
    /// Tra cứu nhật ký hoạt động của hệ thống (ai làm gì, trên đối tượng nào).
    /// </summary>
    public interface IAuditLogAppService : IApplicationService
    {
        /// <summary>
        /// Lấy danh sách log phân trang, mới nhất trước, có tìm kiếm theo
        /// hành động hoặc mã đối tượng.
        /// </summary>
        /// <param name="searchTerm">Từ khóa tìm trong cột Action/ObjectId (trống = không lọc).</param>
        /// <param name="skipCount">Số dòng bỏ qua (phân trang).</param>
        /// <param name="maxResultCount">Số dòng mỗi trang, tối đa 100.</param>
        /// <returns>Kết quả phân trang kèm tổng số dòng.</returns>
        Task<PagedResultDto<AuditLogEntryDto>> GetListAsync(string searchTerm, int skipCount, int maxResultCount);
    }

    /// <summary>
    /// Service tra cứu nhật ký hoạt động cho admin: ai (UserId) đã thực hiện
    /// hành động gì (Action) trên đối tượng nào (ObjectId) lúc nào.
    /// </summary>
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

        /// <summary>
        /// Lấy danh sách log phân trang (xem mô tả ở interface).
        /// </summary>
        public virtual async Task<PagedResultDto<AuditLogEntryDto>> GetListAsync(
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
                // skipCount âm sẽ làm SQL lỗi nên ép về 0; giới hạn 100
                // dòng/trang để bảng log (tăng mỗi ngày) không bị kéo hết
                // về memory khi admin quên truyền page size.
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
