using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    public class PermissionDefinitionModel
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }
    }

    public class RolePermissionsModel
    {
        public string Role { get; set; }
        public string RoleLabel { get; set; }
        public List<string> GrantedKeys { get; set; } = new List<string>();
    }

    public class PermissionMatrixModel
    {
        public List<PermissionDefinitionModel> Definitions { get; set; } = new List<PermissionDefinitionModel>();
        public List<RolePermissionsModel> Roles { get; set; } = new List<RolePermissionsModel>();
    }

    /// <summary>
    /// Item 155: fine-grained permission client. The current user's keys are
    /// fetched once from GET api/auth/my-permissions and cached in memory
    /// for the session; call ClearCache() on login/logout.
    /// </summary>
    public class PermissionService : HttpService
    {
        // Cache chính Task (AsyncLazy pattern): các caller đồng thời chia sẻ
        // 1 lần fetch đang chạy, thay vì mỗi caller đều gọi API riêng trong
        // khoảng giữa lần check và lần set (async double-check race).
        // Fetch lỗi vẫn trả về tập rỗng nên cache không bao giờ bị poison.
        private Task<HashSet<string>> _permissionsTask;
        private readonly object _lock = new object();

        public PermissionService(HttpClient httpClient, ITokenManager tokenManager)
            : base(httpClient, tokenManager)
        {
        }

        public void ClearCache()
        {
            lock (_lock)
            {
                _permissionsTask = null;
            }
        }

        public Task<HashSet<string>> GetMyPermissionsAsync()
        {
            lock (_lock)
            {
                if (_permissionsTask == null)
                {
                    _permissionsTask = FetchMyPermissionsAsync();
                }

                return _permissionsTask;
            }
        }

        private async Task<HashSet<string>> FetchMyPermissionsAsync()
        {
            try
            {
                var response = await GetAsync<MyPermissionsResponse>("api/auth/my-permissions");
                return new HashSet<string>(
                    response?.Permissions ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // Not logged in (or API unreachable): no permissions.
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<bool> HasPermissionAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return (await GetMyPermissionsAsync()).Contains(key);
        }

        public async Task<PermissionMatrixModel> GetMatrixAsync()
            => await GetAsync<PermissionMatrixModel>("api/admin/permissions/matrix");

        public async Task SaveRolePermissionsAsync(string role, List<string> grantedKeys)
            => await PutAsync<object>("api/admin/permissions/matrix",
                new { role, grantedKeys = grantedKeys ?? new List<string>() });

        // CA1852: private nested type không có subtype → sealed để JIT devirtualize.
        private sealed class MyPermissionsResponse
        {
            public List<string> Permissions { get; set; }
        }
    }
}
