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

        Task<bool> HasPermissionAsync(string permissionKey);

        /// <summary>Throws <see cref="AbpAuthorizationException"/> (403) when missing.</summary>
        Task RequirePermissionAsync(string permissionKey);

        /// <summary>Admin: the full role × permission matrix.</summary>
        Task<PermissionMatrixDto> GetMatrixAsync();

        /// <summary>Admin: replaces one role's granted keys.</summary>
        Task UpdateRolePermissionsAsync(UpdateRolePermissionsDto input);
    }

    public class PermissionService : ApplicationService, IPermissionService
    {
        private readonly IRepository<RolePermission, Guid> _rolePermissions;
        private readonly IRepository<UserRole, Guid> _userRoles;
        private readonly IAsyncQueryableExecuter _asyncExecuter;

        public PermissionService(
            IRepository<RolePermission, Guid> rolePermissions,
            IRepository<UserRole, Guid> userRoles,
            IAsyncQueryableExecuter asyncExecuter)
        {
            _rolePermissions = rolePermissions;
            _userRoles = userRoles;
            _asyncExecuter = asyncExecuter;
        }

        public async Task<HashSet<string>> GetUserPermissionsAsync()
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

        public async Task<bool> HasPermissionAsync(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                return false;
            }

            var granted = await GetUserPermissionsAsync();
            return granted.Contains(permissionKey);
        }

        public async Task RequirePermissionAsync(string permissionKey)
        {
            if (!await HasPermissionAsync(permissionKey))
            {
                var label = PermissionKeys.All
                    .FirstOrDefault(d => string.Equals(d.Key, permissionKey, StringComparison.OrdinalIgnoreCase))
                    ?.Label ?? permissionKey;
                throw new AbpAuthorizationException($"Bạn không có quyền \"{label}\".");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<PermissionMatrixDto> GetMatrixAsync()
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
                    RoleLabel = RoleLabels.Get(role),
                    GrantedKeys = all
                        .Where(rp => string.Equals(rp.Role, role, StringComparison.OrdinalIgnoreCase))
                        .Select(rp => rp.PermissionKey)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                }).ToList(),
            };
        }

        [Authorize(Roles = "Admin")]
        public async Task UpdateRolePermissionsAsync(UpdateRolePermissionsDto input)
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

            foreach (var row in existing.Where(rp => !wanted.Contains(rp.PermissionKey, StringComparer.OrdinalIgnoreCase)))
            {
                await _rolePermissions.DeleteAsync(row);
            }

            foreach (var key in wanted.Where(k => !existingKeys.Contains(k)))
            {
                await _rolePermissions.InsertAsync(new RolePermission
                {
                    Role = role,
                    PermissionKey = key,
                    CreatedAt = DateTime.UtcNow,
                });
            }
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
