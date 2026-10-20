using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace PhuThuongStickyRice.Application.Permissions
{
    /// <summary>
    /// Item 155: fine-grained permission checks. Grants live in the
    /// RolePermissions table (editable at /admin/phan-quyen); Admin
    /// implicitly holds every key. All checks resolve roles from the
    /// database — never trust client-supplied claims for this.
    /// </summary>
    public interface IPermissionService : IApplicationService
    {
        /// <summary>All permission keys the current user holds.</summary>
        Task<HashSet<string>> GetUserPermissionsAsync();

        /// <summary>
        /// Kiểm tra user hiện tại có giữ key quyền không (gộp từ mọi role
        /// của user; role Admin luôn true không cần dòng DB).
        /// </summary>
        /// <param name="permissionKey">Key quyền cần kiểm tra, vd "orders.refund".</param>
        Task<bool> HasPermissionAsync(string permissionKey);

        /// <summary>Throws <see cref="AbpAuthorizationException"/> (403) when missing.</summary>
        Task RequirePermissionAsync(string permissionKey);

        /// <summary>Admin: the full role × permission matrix.</summary>
        Task<PermissionMatrixDto> GetMatrixAsync();

        /// <summary>Admin: replaces one role's granted keys.</summary>
        Task UpdateRolePermissionsAsync(UpdateRolePermissionsDto input);
    }

    /// <summary>
    /// Phân quyền chi tiết cho nhân viên (vd "orders.refund"): mỗi user được
    /// gộp quyền từ tất cả role của mình; role Admin luôn có toàn bộ quyền.
    /// </summary>
    public class PermissionService : ApplicationService, IPermissionService
    {
        private readonly IRepository<RolePermission, Guid> _rolePermissions;
        private readonly IRepository<UserRole, Guid> _userRoles;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        /// <summary>Khởi tạo service phân quyền nhân viên.</summary>
        /// <param name="rolePermissions">Repository quyền của role.</param>
        /// <param name="userRoles">Repository gán role cho user.</param>
        /// <param name="asyncExecuter">Thực thi truy vấn bất đồng bộ.</param>
        public PermissionService(
            IRepository<RolePermission, Guid> rolePermissions,
            IRepository<UserRole, Guid> userRoles,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _rolePermissions = rolePermissions;
            _userRoles = userRoles;
            _asyncExecuter = asyncExecuter;
        }

        /// <summary>
        /// Tập quyền của user đang đăng nhập: gộp từ mọi role của user.
        /// Role Admin luôn có toàn bộ quyền (không cần gán từng key) để không
        /// bao giờ bị khóa khỏi tính năng khi admin thêm key quyền mới.
        /// </summary>
        /// <returns>Tập key quyền (so sánh không phân biệt hoa thường).</returns>
        public virtual async Task<HashSet<string>> GetUserPermissionsAsync()
        {
            var roles = await GetCurrentRoleNamesAsync();

            if (roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return PermissionKeys.All.Select(d => d.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            if (roles.Length == 0)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var queryable = await _rolePermissions.GetQueryableAsync();
            var keys = await _asyncExecuter.ToListAsync(
                queryable.Where(rp => roles.Contains(rp.Role)).Select(rp => rp.PermissionKey));

            return keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra user đang đăng nhập có một quyền cụ thể không.
        /// </summary>
        /// <param name="permissionKey">Key quyền (vd "orders.refund"); rỗng thì coi như không có.</param>
        /// <returns>true khi user được cấp quyền.</returns>
        public virtual async Task<bool> HasPermissionAsync(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            var granted = await GetUserPermissionsAsync();
            return granted.Contains(permissionKey);
        }

        /// <summary>
        /// Bắt buộc quyền: ném lỗi phân quyền khi user không có quyền.
        /// Dùng đầu mỗi action nhạy cảm (hoàn tiền, xóa...) thay vì check tay.
        /// </summary>
        /// <param name="permissionKey">Key quyền yêu cầu.</param>
        /// <exception cref="Volo.Abp.Authorization.AbpAuthorizationException">Ném khi user thiếu quyền.</exception>
        public virtual async Task RequirePermissionAsync(string permissionKey)
        {
            if (!await HasPermissionAsync(permissionKey))
            {
                var label = PermissionKeys.All
                    .FirstOrDefault(d => string.Equals(d.Key, permissionKey, StringComparison.OrdinalIgnoreCase))
                    ?.Label ?? permissionKey;
                throw new AbpAuthorizationException($"Bạn không có quyền \"{label}\".");
            }
        }

        /// <summary>
        /// Admin: dựng ma trận role × quyền cho trang /admin/phan-quyen —
        /// định nghĩa quyền lấy từ PermissionKeys.All, danh sách role từ
        /// PermissionKeys.MatrixRoles, quyền đã cấp đọc từ bảng
        /// RolePermissions trong DB. Role Admin không cần dòng DB vì luôn
        /// có toàn quyền.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public virtual async Task<PermissionMatrixDto> GetMatrixAsync()
        {
            var queryable = await _rolePermissions.GetQueryableAsync();
            var all = await _asyncExecuter.ToListAsync(queryable);

            return new PermissionMatrixDto
            {
                Definitions = PermissionKeys.All.Select(d => new PermissionDefinitionDto
                {
                    Key = d.Key,
                    Label = d.Label,
                    Group = d.Group,
                }).ToList(),
                Roles = PermissionKeys.MatrixRoles.Select(role => new RolePermissionsDto
                {
                    Role = role,
                    RoleLabel = RoleLabels.GetLabel(role),
                    GrantedKeys = all
                        .Where(rp => string.Equals(rp.Role, role, StringComparison.OrdinalIgnoreCase))
                        .Select(rp => rp.PermissionKey)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                }).ToList(),
            };
        }

        /// <summary>
        /// Admin: thay thế toàn bộ key quyền của 1 role (full-replace — xóa
        /// dòng không còn trong danh sách mới, thêm dòng mới) thay vì sửa
        /// từng dòng, để ma trận hiển thị luôn khớp DB. Chỉ chấp nhận role
        /// trong PermissionKeys.MatrixRoles và key hợp lệ trong
        /// PermissionKeys.All; role Admin không chỉnh được (luôn full quyền).
        /// </summary>
        /// <param name="input">Role cần phân quyền và danh sách key được cấp.</param>
        [Authorize(Roles = "Admin")]
        public virtual async Task UpdateRolePermissionsAsync(UpdateRolePermissionsDto input)
        {
            var role = (input?.Role ?? string.Empty).Trim();
            if (!PermissionKeys.MatrixRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                throw new Volo.Abp.BusinessException("ROLE_NOT_EDITABLE",
                    $"Chỉ được phân quyền cho: {string.Join(", ", PermissionKeys.MatrixRoles)}. Admin luôn có toàn quyền.");
            }

            var validKeys = PermissionKeys.All.Select(d => d.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var wanted = (input.GrantedKeys ?? new List<string>())
                .Where(k => validKeys.Contains(k))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var queryable = await _rolePermissions.GetQueryableAsync();
            var existing = await _asyncExecuter.ToListAsync(
                queryable.Where(rp => rp.Role == role));

            var existingKeys = existing.Select(rp => rp.PermissionKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Best practice: xóa/chèn hàng loạt 1 lần thay vì DeleteAsync/
            // InsertAsync từng dòng trong vòng lặp — cùng semantics (UoW commit
            // 1 lần cuối method như cũ), code thể hiện đúng ý định full-replace.
            // Guid Id của dòng mới vẫn được Repository tự sinh (stamp Guid khi
            // Id rỗng) như khi InsertAsync từng dòng trước đây.
            var rowsToDelete = existing
                .Where(rp => !wanted.Contains(rp.PermissionKey, StringComparer.OrdinalIgnoreCase))
                .ToList();
            await _rolePermissions.DeleteManyAsync(rowsToDelete);

            var rowsToInsert = wanted
                .Where(k => !existingKeys.Contains(k))
                .Select(key => new RolePermission
                {
                    Role = role,
                    PermissionKey = key,
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();
            await _rolePermissions.InsertManyAsync(rowsToInsert);
        }

        /// <summary>
        /// Resolves the current user's role names from the database
        /// (same approach as AuthController — the template UserStore does
        /// not implement IUserRoleStore).
        /// </summary>
        private async Task<string[]> GetCurrentRoleNamesAsync()
        {
            if (CurrentUser?.Id == null)
            {
                return Array.Empty<string>();
            }

            var userId = CurrentUser.Id.Value;
            var queryable = await _userRoles.GetQueryableAsync();
            var roles = await _asyncExecuter.ToListAsync(
                queryable.Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name));

            return roles
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
