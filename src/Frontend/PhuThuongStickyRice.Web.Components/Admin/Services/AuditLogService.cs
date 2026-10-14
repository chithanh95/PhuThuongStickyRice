using PhuThuongStickyRice.Web.Components.Admin.Models;
using PhuThuongStickyRice.Web.Components.Core.Services;
using PhuThuongStickyRice.Web.Components.Shop.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
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
