using Microsoft.AspNetCore.Identity;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
    public class HistoricalPasswordValidator : IPasswordValidator<User>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string password)
        {
            // CA1307: chỉ rõ StringComparison.Ordinal cho string.Contains —
            // .NET Core mặc định đã là Ordinal nhưng viết tường minh để
            // analyzer và người đọc sau không phải đoán ngữ nghĩa culture.
            if (password.Contains("testhistoricalpassword", StringComparison.Ordinal))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "HistoricalPassword",
                    Description = "HistoricalPasswordValidator testing.",
                }));
            }

            // TODO: check password histories, etc.
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
