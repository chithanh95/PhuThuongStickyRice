using PhuThuongStickyRice.Web.Components.Core.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Web.Components.Admin.Services
{
    public class StaffUserModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsLocked { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class StaffService : ApiClientBase
    {
        public StaffService(HttpClient httpClient, ITokenStore tokenStore)
            : base(httpClient, tokenStore)
        {
        }

        public Task<List<StaffUserModel>> GetUsersAsync()
            => GetAsync<List<StaffUserModel>>("api/staff/users");

        public Task CreateUserAsync(string userName, string email, string password, string role)
            => PostAsync<object>("api/staff/users", new { userName, email, password, role });

        public Task AssignRoleAsync(Guid userId, string role)
            => PostAsync<object>($"api/staff/users/{userId:D}/roles", new { role });

        public Task RemoveRoleAsync(Guid userId, string role)
            => DeleteAsync($"api/staff/users/{userId:D}/roles/{Uri.EscapeDataString(role)}");
    }
}
