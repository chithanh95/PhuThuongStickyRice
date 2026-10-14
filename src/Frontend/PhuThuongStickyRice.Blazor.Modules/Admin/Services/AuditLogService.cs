using PhuThuongStickyRice.Blazor.Modules.Admin.Models;
using PhuThuongStickyRice.Blazor.Modules.Core.Services;
using PhuThuongStickyRice.Blazor.Modules.Shop.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Blazor.Modules.Admin.Services
{
    public class AuditLogService : HttpService
    {
        public AuditLogService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public async Task<PagedResult<AuditLogEntryModel>> GetListAsync(
            int page = 1, int pageSize = 20, string searchTerm = null)
        {
            var url = $"api/audit-logs?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&searchTerm={System.Uri.EscapeDataString(searchTerm)}";
            }

            return await GetAsync<PagedResult<AuditLogEntryModel>>(url);
        }
    }
}
