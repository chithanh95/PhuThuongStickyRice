using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
    // [codequality-docs 2026-10-19]
    /// <summary>
    /// Triển khai <see cref="IRoleStore{Role}"/> cho ASP.NET Identity trên entity Role.
    /// </summary>
    /// <remarks>
    /// Hiện tại app không quản lý role qua ASP.NET Identity (phân quyền dùng cơ chế
    /// khác), nên tất cả method của store này chủ ý ném <see cref="NotImplementedException"/>
    /// nếu bị gọi — đó là hành vi thiết kế, không phải code dở dang.
    /// </remarks>
    public class RoleStore : IRoleStore<Role>
    {
        private readonly IRepository<Role, Guid> _roleRepository;

        /// <summary>Khởi tạo role store (giữ repository để dùng khi triển khai đầy đủ).</summary>
        // <param name="roleRepository">Repository entity Role.</param>
        public RoleStore(IRepository<Role, Guid> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        /// <summary>Giải phóng tài nguyên — store này không giữ tài nguyên nào.</summary>
        public void Dispose()
        {
        }

        /// <summary>Tạo role mới. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Xóa role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Tìm role theo id. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<Role> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Tìm role theo tên chuẩn hóa. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<Role> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Lấy tên chuẩn hóa của role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<string> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Lấy id của role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Lấy tên của role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<string> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Gán tên chuẩn hóa cho role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task SetNormalizedRoleNameAsync(Role role, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Gán tên cho role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task SetRoleNameAsync(Role role, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>Cập nhật role. Hiện chưa triển khai (ném <see cref="NotImplementedException"/>).</summary>
        public Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
