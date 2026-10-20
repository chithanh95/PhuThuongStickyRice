using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

// Batch marker: [cq-doc-20261020-0037] — XML doc tiếng Việt (Agent 4 Documentation, run 00:37).

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>
    /// Định nghĩa 1 quyền chi tiết (vd "orders.confirm"): khóa máy + nhãn
    /// hiển thị tiếng Việt + nhóm gom quyền trên màn hình phân quyền.
    /// </summary>
    public class PermissionDefinitionModel
    {
        /// <summary>Khóa quyền dùng khi kiểm tra (vd "orders.confirm").</summary>
        public string Key { get; set; }

        /// <summary>Nhãn hiển thị tiếng Việt trên màn hình phân quyền.</summary>
        public string Label { get; set; }

        /// <summary>Nhóm gom quyền (vd "Đơn hàng", "Nhân viên").</summary>
        public string Group { get; set; }
    }

    /// <summary>
    /// Tập quyền đã cấp cho 1 vai trò (Admin/Cashier/Kitchen) — dữ liệu cột
    /// của ma trận phân quyền ở màn hình admin.
    /// </summary>
    public class RolePermissionsModel
    {
        /// <summary>Tên vai trò (khóa): Admin, Cashier, Kitchen.</summary>
        public string Role { get; set; }

        /// <summary>Nhãn vai trò hiển thị tiếng Việt.</summary>
        public string RoleLabel { get; set; }

        /// <summary>Danh sách khóa quyền đã được cấp cho vai trò này.</summary>
        public List<string> GrantedKeys { get; set; } = new List<string>();
    }

    /// <summary>
    /// Toàn bộ ma trận phân quyền: danh sách quyền định nghĩa + quyền của
    /// từng vai trò — nguồn dữ liệu cho màn hình quản lý phân quyền admin.
    /// </summary>
    public class PermissionMatrixModel
    {
        /// <summary>Tất cả quyền định nghĩa trong hệ thống (theo nhóm).</summary>
        public List<PermissionDefinitionModel> Definitions { get; set; } = new List<PermissionDefinitionModel>();

        /// <summary>Quyền đã cấp cho từng vai trò.</summary>
        public List<RolePermissionsModel> Roles { get; set; } = new List<RolePermissionsModel>();
    }

    /// <summary>
    /// Item 155: fine-grained permission client. The current user's keys are
    /// fetched once from GET api/auth/my-permissions and cached in memory
    /// for the session; call ClearCache() on login/logout.
    /// </summary>
    public class PermissionService : ApiClientBase
    {
        // Cache chính Task (AsyncLazy pattern): các caller đồng thời chia sẻ
        // 1 lần fetch đang chạy, thay vì mỗi caller đều gọi API riêng trong
        // khoảng giữa lần check và lần set (async double-check race).
        // Fetch lỗi vẫn trả về tập rỗng nên cache không bao giờ bị poison.
        private Task<HashSet<string>> _permissionsTask;
        private readonly object _permissionsLock = new object();

        public PermissionService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>
        /// Xóa cache quyền trong memory — gọi khi đăng nhập/đăng xuất để
        /// user mới không dùng nhầm quyền của user cũ.
        /// </summary>
        public void ClearCache()
        {
            lock (_permissionsLock)
            {
                _permissionsTask = null;
            }
        }

        /// <summary>
        /// Lấy tập khóa quyền của user đang đăng nhập (fetch 1 lần rồi cache
        /// cả session). Trả về tập rỗng khi chưa đăng nhập hoặc API lỗi.
        /// </summary>
        public Task<HashSet<string>> GetMyPermissionsAsync()
        {
            lock (_permissionsLock)
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
                var response = await GetAsync<PermissionsApiResponse>("api/auth/my-permissions");
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

        /// <summary>
        /// Kiểm tra user hiện tại có quyền theo khóa không (không phân biệt
        /// hoa/thường). Khóa rỗng/trắng luôn trả về false.
        /// </summary>
        /// <param name="key">Khóa quyền cần kiểm tra (vd "orders.confirm").</param>
        public async Task<bool> HasPermissionAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return (await GetMyPermissionsAsync()).Contains(key);
        }

        /// <summary>
        /// Tải ma trận phân quyền đầy đủ (danh sách quyền + quyền từng vai trò)
        /// từ API admin — nguồn dữ liệu cho màn hình phân quyền.
        /// </summary>
        public async Task<PermissionMatrixModel> GetMatrixAsync()
            => await GetAsync<PermissionMatrixModel>("api/admin/permissions/matrix");

        /// <summary>
        /// Lưu danh sách quyền được cấp cho 1 vai trò lên server.
        /// </summary>
        /// <param name="role">Tên vai trò (Admin/Cashier/Kitchen).</param>
        /// <param name="grantedKeys">Danh sách khóa quyền được cấp; null coi như rỗng.</param>
        public async Task SaveRolePermissionsAsync(string role, List<string> grantedKeys)
            => await PutAsync<object>("api/admin/permissions/matrix",
                new { role, grantedKeys = grantedKeys ?? new List<string>() });

        // CA1852: private nested type không có subtype → sealed để JIT devirtualize.
        private sealed class PermissionsApiResponse
        {
            public List<string> Permissions { get; set; }
        }
    }
}
