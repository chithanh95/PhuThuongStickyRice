using PhuThuongStickyRice.Application.AuditLogEntries.DTOs;
using PhuThuongStickyRice.Application.Common.DTOs;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.AuditLogs.Services
{
    public class AuditLogService : HttpService
    {
        public AuditLogService(HttpClient httpClient, ITokenManager tokenManager) 
            : base(httpClient, tokenManager)
        {
        }

        public async Task<List<AuditLogEntryDTO>> GetAuditLogsAsync()
        {
            var logs = await GetAsync<List<AuditLogEntryDTO>>("api/auditLogEntries");
            return logs;
        }

        public async Task<Paged<AuditLogEntryDTO>> GetAuditLogsAsync(int page, int pageSize)
        {
            var logs = await GetAsync<Paged<AuditLogEntryDTO>>($"api/auditLogEntries/paged?page={page}&pageSize={pageSize}");
            return logs;
        }
    }
}
