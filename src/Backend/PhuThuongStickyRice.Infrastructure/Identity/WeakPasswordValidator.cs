using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
    public class WeakPasswordValidator : IPasswordValidator<User>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string password)
        {
            // Marker test cố định bằng ASCII — dùng Ordinal để so sánh
            // byte-chính-xác, tránh phụ thuộc culture hiện hành của server
            // (Contains(string) mặc định là so sánh linguistic).
            if (password.Contains("testweakpassword", StringComparison.Ordinal))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "WeakPassword",
                    Description = "WeakPasswordValidator testing.",
                }));
            }

            // TODO: check weak password, leaked password, password histories, etc.
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
