using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    /// <summary>Thông tin nhân viên (tài khoản đăng nhập trang admin).</summary>
    public class StaffUserModel
    {
        /// <summary>Id tài khoản.</summary>
        public Guid Id { get; set; }

        /// <summary>Tên đăng nhập.</summary>
        public string UserName { get; set; }

        /// <summary>Email của nhân viên.</summary>
        public string Email { get; set; }

        /// <summary>SĐT của nhân viên.</summary>
        public string PhoneNumber { get; set; }

        /// <summary>True khi tài khoản đang bị khóa (không đăng nhập được).</summary>
        public bool IsLocked { get; set; }

        /// <summary>Các vai trò của tài khoản (Admin, Cashier...).</summary>
        public List<string> Roles { get; set; } = new();
    }

    /// <summary>
    /// Admin: quản lý tài khoản nhân viên (api/staff/users) — liệt kê, tạo mới,
    /// gán và gỡ vai trò. Chỉ Admin mới gọi được các API này.
    /// </summary>
    public class StaffService : ApiClientBase
    {
        /// <summary>Khởi tạo service quản lý nhân viên với HttpClient và token store.</summary>
        public StaffService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        /// <summary>Admin: danh sách tài khoản nhân viên.</summary>
        /// <returns>Danh sách nhân viên kèm vai trò và trạng thái khóa.</returns>
        public Task<List<StaffUserModel>> GetUsersAsync()
            => GetAsync<List<StaffUserModel>>("api/staff/users");

        /// <summary>Admin: tạo tài khoản nhân viên mới.</summary>
        /// <param name="userName">Tên đăng nhập.</param>
        /// <param name="email">Email của nhân viên.</param>
        /// <param name="password">Mật khẩu ban đầu.</param>
        /// <param name="role">Vai trò gán ngay khi tạo (Admin/Cashier...).</param>
        public Task CreateUserAsync(string userName, string email, string password, string role)
            => PostAsync<object>("api/staff/users", new { userName, email, password, role });

        /// <summary>Admin: gán thêm vai trò cho nhân viên.</summary>
        /// <param name="userId">Id tài khoản nhân viên.</param>
        /// <param name="role">Vai trò cần gán.</param>
        public Task AssignRoleAsync(Guid userId, string role)
            => PostAsync<object>($"api/staff/users/{userId:D}/roles", new { role });

        /// <summary>Admin: gỡ vai trò khỏi nhân viên.</summary>
        /// <param name="userId">Id tài khoản nhân viên.</param>
        /// <param name="role">Vai trò cần gỡ.</param>
        public Task RemoveRoleAsync(Guid userId, string role)
            => DeleteAsync($"api/staff/users/{userId:D}/roles/{Uri.EscapeDataString(role)}");
    }
}
