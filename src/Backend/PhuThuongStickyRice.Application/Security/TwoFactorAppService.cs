
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using OtpNet;
using QRCoder;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using PhuThuongStickyRice.Application.Security;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.Application.Security
{
    /// <summary>
    /// Item 98: admin TOTP 2FA. Setup shows an otpauth:// QR (QRCoder) and the
    /// admin verifies a 6-digit code to enable. The login flow calls
    /// <see cref="VerifyCodeAsync"/> through the auth controller.
    /// </summary>
    public interface ITwoFactorAppService : IApplicationService
    {
        /// <summary>
        /// Admin: trả về QR + secret để cài đặt 2FA trên app xác thực.
        /// Secret chỉ hiển thị, chưa lưu vào DB cho đến khi admin nhập đúng
        /// mã 6 số qua <see cref="EnableAsync"/> — tránh bật nhầm khi chưa
        /// cấu hình xong app.
        /// </summary>
        /// <returns>Secret, URI otpauth và QR dạng data URL cùng trạng thái hiện tại.</returns>
        Task<TwoFactorSetupDto> GetSetupAsync();

        /// <summary>
        /// Admin: bật 2FA sau khi admin nhập đúng mã 6 số từ app xác thực
        /// (secret phải khớp secret đã hiển thị ở <see cref="GetSetupAsync"/>).
        /// </summary>
        /// <param name="input">Secret và mã xác thực 6 số.</param>
        /// <returns>Trạng thái cài đặt sau khi bật.</returns>
        Task<TwoFactorSetupDto> EnableAsync(EnableTwoFactorDto input);

        /// <summary>
        /// Admin: tắt 2FA. Nếu 2FA đang bật, yêu cầu nhập mã xác thực hợp lệ
        /// để tránh ai đó vô tình (hoặc kẻ lạ chiếm session) tắt mất lớp bảo vệ.
        /// </summary>
        /// <param name="input">Mã xác thực 6 số từ app.</param>
        Task DisableAsync(VerifyTwoFactorDto input);

        /// <summary>Internal: validates a TOTP code for the user (login flow).</summary>
        Task<bool> VerifyCodeAsync(Guid userId, string code);
    }

    [Authorize(Roles = "Admin")]
    public class TwoFactorAppService : ApplicationService, ITwoFactorAppService
    {
        /// <summary>Tên issuer hiển thị trong app xác thực (Google Authenticator, Microsoft Authenticator...).</summary>
        private const string Issuer = "XoiPhuThuong";

        private readonly IRepository<User, Guid> _users;

        public TwoFactorAppService(IRepository<User, Guid> users)
        {
            _users = users;
        }

        /// <summary>
        /// Admin: lấy QR + secret để cài đặt 2FA. Giữ secret hiện có cho đến
        /// khi verify thành công mã mới — tránh trường hợp admin quét QR dở
        /// dang làm mất secret cũ.
        /// </summary>
        /// <returns>Secret, URI otpauth và QR dạng data URL cùng trạng thái hiện tại.</returns>
        public virtual async Task<TwoFactorSetupDto> GetSetupAsync()
        {
            var user = await GetCurrentUserAsync();

            // Keep the existing secret until the admin successfully verifies
            // a new one; generate a fresh secret when none is stored.
            var secret = user.TotpSecret;
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            }

            var uri = BuildOtpAuthUri(user.UserName ?? user.Email ?? "admin", secret);
            var png = TableQrAppService.RenderPng(uri);

            return new TwoFactorSetupDto
            {
                Secret = secret,
                OtpAuthUri = uri,
                QrDataUrl = "data:image/png;base64," + Convert.ToBase64String(png),
                IsEnabled = user.TwoFactorEnabled
            };
        }

        /// <summary>
        /// Admin: bật 2FA sau khi nhập đúng mã 6 số. Secret chỉ được lưu vào
        /// DB sau khi verify thành công — nhập sai mã thì không thay đổi gì.
        /// </summary>
        /// <param name="input">Secret và mã xác thực 6 số.</param>
        /// <returns>Trạng thái cài đặt sau khi bật.</returns>
        public virtual async Task<TwoFactorSetupDto> EnableAsync(EnableTwoFactorDto input)
        {
            if (string.IsNullOrWhiteSpace(input.Secret))
            {
                throw new Domain.Exceptions.DomainException("Thiếu mã bí mật 2FA.");
            }

            if (!VerifyCode(input.Secret.Trim(), input.Code))
            {
                throw new Domain.Exceptions.DomainException("Mã xác thực 2FA không đúng. Vui lòng thử lại.");
            }

            var user = await GetCurrentUserAsync();
            user.TotpSecret = input.Secret.Trim();
            user.TwoFactorEnabled = true;
            await _users.UpdateAsync(user, autoSave: true);

            return new TwoFactorSetupDto { IsEnabled = true };
        }

        /// <summary>
        /// Admin: tắt 2FA. Khi 2FA đang bật, bắt buộc nhập đúng mã xác thực
        /// để phòng trường hợp session bị chiếm hoặc tắt nhầm.
        /// </summary>
        /// <param name="input">Mã xác thực 6 số từ app.</param>
        public virtual async Task DisableAsync(VerifyTwoFactorDto input)
        {
            var user = await GetCurrentUserAsync();
            if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                if (!VerifyCode(user.TotpSecret, input.Code))
                {
                    throw new Domain.Exceptions.DomainException("Mã xác thực 2FA không đúng.");
                }
            }

            user.TotpSecret = null;
            user.TwoFactorEnabled = false;
            await _users.UpdateAsync(user, autoSave: true);
        }

        /// <summary>Internal — no Admin policy so the login flow can call it.</summary>
        [AllowAnonymous]
        public virtual async Task<bool> VerifyCodeAsync(Guid userId, string code)
        {
            var user = await _users.FindAsync(userId);
            if (user == null || !user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                return false;
            }

            return VerifyCode(user.TotpSecret, code);
        }

        /// <summary>
        /// Kiểm tra mã TOTP 6 số với secret base32. Chấp nhận lệch giờ đồng hồ
        /// 1 bước mỗi chiều (VerificationWindow(1,1) ≈ ±30s) vì điện thoại admin
        /// và server hiếm khi đồng bộ tuyệt đối. Mã lỗi/sai định dạng trả về
        /// false thay vì ném lỗi — login flow chỉ cần biết đúng/sai.
        /// </summary>
        private static bool VerifyCode(string base32Secret, string code)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(base32Secret.Trim().Replace(" ", string.Empty).ToUpperInvariant());
                var totp = new Totp(secretBytes);
                // Allow one step of clock drift each way.
                return totp.VerifyTotp((code ?? string.Empty).Trim(), out _, new VerificationWindow(1, 1));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Dựng URI otpauth:// theo chuẩn TOTP (digits=6, period=30s) để app
        /// xác thực quét QR và hiểu đúng thuật toán.
        /// </summary>
        private static string BuildOtpAuthUri(string accountName, string base32Secret)
        {
            var label = Uri.EscapeDataString($"{Issuer}:{accountName}");
            return $"otpauth://totp/{label}?secret={base32Secret}&issuer={Uri.EscapeDataString(Issuer)}&digits=6&period=30";
        }

        /// <summary>Lấy user admin đang đăng nhập; ném lỗi nếu chưa xác định được.</summary>
        private async Task<Domain.Entities.User> GetCurrentUserAsync()
        {
            var userId = CurrentUser?.Id;
            if (!userId.HasValue)
            {
                throw new Domain.Exceptions.DomainException("Không xác định được người dùng.");
            }

            var user = await _users.FindAsync(userId.Value);
            if (user == null)
            {
                throw new Domain.Exceptions.DomainException("Không tìm thấy người dùng.");
            }

            return user;
        }
    }
}
