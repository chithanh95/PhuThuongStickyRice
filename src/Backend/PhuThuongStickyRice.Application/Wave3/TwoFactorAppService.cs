using PhuThuongStickyRice.Application.Wave3.DTOs;
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using OtpNet;
using QRCoder;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace PhuThuongStickyRice.Application.Wave3
{
    /// <summary>
    /// Item 98: admin TOTP 2FA. Setup shows an otpauth:// QR (QRCoder) and the
    /// admin verifies a 6-digit code to enable. The login flow calls
    /// <see cref="VerifyCodeAsync"/> through the auth controller.
    /// </summary>
    public interface ITwoFactorAppService : IApplicationService
    {
        Task<TwoFactorSetupDto> GetSetupAsync();

        Task<TwoFactorSetupDto> EnableAsync(EnableTwoFactorDto input);

        Task DisableAsync(VerifyTwoFactorDto input);

        /// <summary>Internal: validates a TOTP code for the user (login flow).</summary>
        Task<bool> VerifyCodeAsync(Guid userId, string code);
    }

    [Authorize(Roles = "Admin")]
    public class TwoFactorAppService : ApplicationService, ITwoFactorAppService
    {
        private const string Issuer = "XoiPhuThuong";

        private readonly IRepository<User, Guid> _users;

        public TwoFactorAppService(IRepository<User, Guid> users)
        {
            _users = users;
        }

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

        private static string BuildOtpAuthUri(string accountName, string base32Secret)
        {
            var label = Uri.EscapeDataString($"{Issuer}:{accountName}");
            return $"otpauth://totp/{label}?secret={base32Secret}&issuer={Uri.EscapeDataString(Issuer)}&digits=6&period=30";
        }

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
