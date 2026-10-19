using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Identity
{
        // [codequality-docs 2026-10-19]
        /// <summary>
        /// Lớp lưu trữ người dùng cho ASP.NET Identity trên entity User của quán.
        /// </summary>
        /// <remarks>
        /// Triển khai đầy đủ các store: tài khoản, mật khẩu, security stamp,
        /// email, SĐT, xác thực 2 bước (authenticator + mã khôi phục), lockout
        /// và token xác thực — để Identity hoạt động trực tiếp trên schema của quán
        /// thay vì schema mặc định của ASP.NET Identity.
        /// </remarks>
    public class UserStore : IUserStore<User>,
                             IUserPasswordStore<User>,
                             IUserSecurityStampStore<User>,
                             IUserEmailStore<User>,
                             IUserPhoneNumberStore<User>,
                             IUserTwoFactorStore<User>,
                             IUserLockoutStore<User>,
                             IUserAuthenticationTokenStore<User>,
                             IUserAuthenticatorKeyStore<User>,
                             IUserTwoFactorRecoveryCodeStore<User>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;

        /// <summary>Khởi tạo user store.</summary>
        /// <param name="userRepository">Repository entity User (cung cấp luôn UnitOfWork để lưu).</param>
        public UserStore(IUserRepository userRepository)
        {
            _unitOfWork = userRepository.UnitOfWork;
            _userRepository = userRepository;
        }

        /// <summary>Giải phóng tài nguyên — store này không giữ tài nguyên cần dispose.</summary>
        public void Dispose()
        {
        }

        /// <summary>Tạo tài khoản mới: đồng thời lưu mật khẩu hiện tại vào lịch sử mật khẩu (để chặn dùng lại mật khẩu cũ), rồi lưu qua repository.</summary>
        public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
        {
            user.PasswordHistories = new List<PasswordHistory>()
            {
                new PasswordHistory
                {
                    PasswordHash = user.PasswordHash,
                    CreatedDateTime = DateTimeOffset.Now,
                },
            };
            await _userRepository.AddOrUpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        /// <summary>Xóa tài khoản.</summary>
        public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
        {
            _userRepository.Delete(user);
            return Task.FromResult(IdentityResult.Success);
        }

        /// <summary>Tìm người dùng theo email đã chuẩn hóa.</summary>
        public Task<User> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return _userRepository.Get(new UserQueryOptions { IncludeTokens = true }).FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        /// <summary>Tìm người dùng theo id.</summary>
        public Task<User> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            // Guid sai định dạng (vd: cookie/claim bị giả mạo) thì trả null theo đúng
            // contract của ASP.NET Identity ("not found"), thay vì ném FormatException.
            if (!Guid.TryParse(userId, out var id))
            {
                return Task.FromResult<User>(null);
            }

            return _userRepository.Get(new UserQueryOptions { IncludeTokens = true }).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <summary>Tìm người dùng theo tên đăng nhập đã chuẩn hóa.</summary>
        public Task<User> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return _userRepository.Get(new UserQueryOptions { IncludeTokens = true }).FirstOrDefaultAsync(x => x.NormalizedUserName == normalizedUserName, cancellationToken);
        }

        /// <summary>Lấy số lần đăng nhập sai liên tiếp (phục vụ khóa tài khoản).</summary>
        public Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>Lấy email của người dùng.</summary>
        public Task<string> GetEmailAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        /// <summary>Kiểm tra email đã được xác thực hay chưa.</summary>
        public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        /// <summary>Kiểm tra tài khoản có cho phép khóa sau nhiều lần đăng nhập sai.</summary>
        public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        /// <summary>Lấy thời điểm hết khóa tài khoản (null = không bị khóa).</summary>
        public Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnd);
        }

        /// <summary>Lấy email đã chuẩn hóa (uppercase).</summary>
        public Task<string> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        /// <summary>Lấy tên đăng nhập đã chuẩn hóa (uppercase).</summary>
        public Task<string> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        /// <summary>Lấy hash mật khẩu hiện tại.</summary>
        public Task<string> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        /// <summary>Lấy số điện thoại.</summary>
        public Task<string> GetPhoneNumberAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        /// <summary>Kiểm tra SĐT đã được xác thực hay chưa.</summary>
        public Task<bool> GetPhoneNumberConfirmedAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        /// <summary>Lấy security stamp (đổi khi thông tin bảo mật thay đổi để vô hiệu hóa cookie cũ).</summary>
        public Task<string> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.SecurityStamp ?? string.Empty);
        }

        /// <summary>Kiểm tra người dùng có bật xác thực 2 bước.</summary>
        public Task<bool> GetTwoFactorEnabledAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }

        /// <summary>Lấy id của người dùng.</summary>
        public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        /// <summary>Lấy tên đăng nhập.</summary>
        public Task<string> GetUserNameAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        /// <summary>Kiểm tra người dùng đã có mật khẩu hay chưa.</summary>
        public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        /// <summary>Tăng bộ đếm đăng nhập sai và trả về số mới.</summary>
        public Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>Reset bộ đếm đăng nhập sai về 0 (sau khi đăng nhập đúng).</summary>
        public Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            return Task.CompletedTask;
        }

        /// <summary>Gán email mới.</summary>
        public Task SetEmailAsync(User user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        /// <summary>Đánh dấu email đã/không được xác thực.</summary>
        public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        /// <summary>Bật/tắt chế độ khóa tài khoản khi đăng nhập sai nhiều lần.</summary>
        public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            return Task.CompletedTask;
        }

        /// <summary>Đặt thời điểm hết khóa tài khoản.</summary>
        public Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            return Task.CompletedTask;
        }

        /// <summary>Gán email đã chuẩn hóa.</summary>
        public Task SetNormalizedEmailAsync(User user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        /// <summary>Gán tên đăng nhập đã chuẩn hóa.</summary>
        public Task SetNormalizedUserNameAsync(User user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        /// <summary>Gán hash mật khẩu mới.</summary>
        public Task SetPasswordHashAsync(User user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        /// <summary>Gán số điện thoại mới.</summary>
        public Task SetPhoneNumberAsync(User user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.CompletedTask;
        }

        /// <summary>Đánh dấu SĐT đã/không được xác thực.</summary>
        public Task SetPhoneNumberConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.CompletedTask;
        }

        /// <summary>Gán security stamp mới.</summary>
        public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        /// <summary>Bật/tắt xác thực 2 bước.</summary>
        public Task SetTwoFactorEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.CompletedTask;
        }

        /// <summary>Gán tên đăng nhập mới.</summary>
        public Task SetUserNameAsync(User user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        /// <summary>Cập nhật thông tin người dùng và lưu DB.</summary>
        public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
        {
            await _userRepository.AddOrUpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return IdentityResult.Success;
        }

        private const string AuthenticatorStoreLoginProvider = "[AuthenticatorStore]";
        private const string AuthenticatorKeyTokenName = "AuthenticatorKey";
        private const string RecoveryCodeTokenName = "RecoveryCodes";

        /// <summary>Lấy token xác thực theo nhà cung cấp và tên token (null nếu không có).</summary>
        public Task<string> GetTokenAsync(User user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var tokenEntity = user.Tokens.SingleOrDefault(
                    l => l.TokenName == name && l.LoginProvider == loginProvider);
            return Task.FromResult(tokenEntity?.TokenValue);
        }

        public async Task SetTokenAsync(User user, string loginProvider, string name, string value, CancellationToken cancellationToken)
        {
            var tokenEntity = user.Tokens.SingleOrDefault(
                    l => l.TokenName == name && l.LoginProvider == loginProvider);
            if (tokenEntity != null)
            {
                tokenEntity.TokenValue = value;
            }
            else
            {
                user.Tokens.Add(new UserToken
                {
                    UserId = user.Id,
                    LoginProvider = loginProvider,
                    TokenName = name,
                    TokenValue = value,
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Xóa token xác thực (chỉ lưu DB nếu token tồn tại).</summary>
        public async Task RemoveTokenAsync(User user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            var tokenEntity = user.Tokens.SingleOrDefault(
                    l => l.TokenName == name && l.LoginProvider == loginProvider);
            if (tokenEntity != null)
            {
                user.Tokens.Remove(tokenEntity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>Gán key authenticator 2FA (lưu dưới dạng token nội bộ).</summary>
        public Task SetAuthenticatorKeyAsync(User user, string key, CancellationToken cancellationToken)
        {
            return SetTokenAsync(user, AuthenticatorStoreLoginProvider, AuthenticatorKeyTokenName, key, cancellationToken);
        }

        /// <summary>Lấy key authenticator 2FA.</summary>
        public Task<string> GetAuthenticatorKeyAsync(User user, CancellationToken cancellationToken)
        {
            return GetTokenAsync(user, AuthenticatorStoreLoginProvider, AuthenticatorKeyTokenName, cancellationToken);
        }

        /// <summary>Thay toàn bộ danh sách mã khôi phục 2FA (lưu nối nhau bằng dấu ';').</summary>
        public Task ReplaceCodesAsync(User user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            var mergedCodes = string.Join(";", recoveryCodes);
            return SetTokenAsync(user, AuthenticatorStoreLoginProvider, RecoveryCodeTokenName, mergedCodes, cancellationToken);
        }

        /// <summary>Dùng một mã khôi phục 2FA: mã dùng thành công bị xóa ngay để không tái sử dụng; trả về true nếu mã hợp lệ.</summary>
        public async Task<bool> RedeemCodeAsync(User user, string code, CancellationToken cancellationToken)
        {
            var mergedCodes = await GetTokenAsync(user, AuthenticatorStoreLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? string.Empty;
            var splitCodes = mergedCodes.Split(';');
            if (splitCodes.Contains(code))
            {
                var updatedCodes = new List<string>(splitCodes.Where(s => s != code));
                await ReplaceCodesAsync(user, updatedCodes, cancellationToken);
                return true;
            }

            return false;
        }

        /// <summary>Đếm số mã khôi phục 2FA còn lại.</summary>
        public async Task<int> CountCodesAsync(User user, CancellationToken cancellationToken)
        {
            var mergedCodes = await GetTokenAsync(user, AuthenticatorStoreLoginProvider, RecoveryCodeTokenName, cancellationToken) ?? string.Empty;
            if (mergedCodes.Length > 0)
            {
                return mergedCodes.Split(';').Length;
            }

            return 0;
        }
    }
}
