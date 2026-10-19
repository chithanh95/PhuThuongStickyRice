
using PhuThuongStickyRice.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using OtpNet;
using QRCoder;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
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
        /// Khi 2FA đã bật: chỉ trả <c>IsEnabled=true</c>, KHÔNG trả secret/QR
        /// (secret thật không bao giờ rời server sau khi đã lưu). Khi chưa bật:
        /// sinh secret mới, lưu tạm server-side (hết hạn 10 phút), trả QR cho
        /// admin quét; <see cref="EnableAsync"/> sẽ đối chiếu mã với secret
        /// đang chờ này chứ không tin secret client gửi lên.
        /// </summary>
        /// <returns>QR + secret đang chờ xác thực (khi chưa bật), hoặc chỉ trạng thái khi đã bật.</returns>
        Task<TwoFactorSetupDto> GetSetupAsync();

        /// <summary>
        /// Admin: bật 2FA sau khi admin nhập đúng mã 6 số từ app xác thực.
        /// Mã được đối chiếu với secret đang chờ trong <see cref="GetSetupAsync"/>
        /// (lưu server-side) — client không được tự chọn secret.
        /// </summary>
        /// <param name="input">Mã xác thực 6 số.</param>
        /// <returns>Trạng thái cài đặt sau khi bật.</returns>
        Task<TwoFactorSetupDto> EnableAsync(VerifyTwoFactorDto input);

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

        /// <summary>
        /// Thời gian secret chờ xác thực còn hiệu lực sau khi <see cref="GetSetupAsync"/>
        /// trả QR — đủ để admin quét và nhập mã, đủ ngắn để phiên chiếm đoạt mau hết hạn.
        /// </summary>
        private static readonly TimeSpan PendingSecretTtl = TimeSpan.FromMinutes(10);

        private readonly IRepository<User, Guid> _users;
        private readonly IDistributedCache<TwoFactorPendingSecretCacheItem> _pendingSecrets;

        public TwoFactorAppService(
            IRepository<User, Guid> users,
            IDistributedCache<TwoFactorPendingSecretCacheItem> pendingSecrets)
        {
            _users = users;
            _pendingSecrets = pendingSecrets;
        }

        /// <summary>
        /// Admin: lấy QR + secret để cài đặt 2FA. Khi 2FA đã bật thì KHÔNG trả
        /// secret/QR (tránh lộ secret thật cho session bị chiếm); khi chưa bật
        /// thì sinh secret mới và giữ server-side cho đến khi <see cref="EnableAsync"/>
        /// xác thực thành công.
        /// </summary>
        /// <returns>QR + secret đang chờ (khi chưa bật), hoặc chỉ trạng thái khi đã bật.</returns>
        public virtual async Task<TwoFactorSetupDto> GetSetupAsync()
        {
            var user = await GetCurrentUserAsync();

            // Bug-hunt 2026-10-19: không bao giờ trả secret thật khi 2FA đã bật —
            // session bị chiếm trước đây đọc được secret và tự sinh mã TOTP mãi mãi.
            // Muốn đổi thiết bị: tắt 2FA (cần mã xác thực) rồi quét QR mới.
            if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                return new TwoFactorSetupDto { IsEnabled = true };
            }

            var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
            await _pendingSecrets.SetAsync(
                PendingKey(user.Id),
                new TwoFactorPendingSecretCacheItem { Secret = secret },
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = PendingSecretTtl });

            var uri = BuildOtpAuthUri(user.UserName ?? user.Email ?? "admin", secret);
            var png = TableQrAppService.RenderPng(uri);

            return new TwoFactorSetupDto
            {
                Secret = secret,
                OtpAuthUri = uri,
                QrDataUrl = "data:image/png;base64," + Convert.ToBase64String(png),
                IsEnabled = false
            };
        }

        /// <summary>
        /// Admin: bật 2FA sau khi nhập đúng mã 6 số. Secret lấy từ phiên chờ
        /// server-side của <see cref="GetSetupAsync"/> — client không được tự
        /// chọn secret (trước đây kẻ chiếm session gửi secret của chính hắn để
        /// khóa vĩnh viễn tài khoản admin thật).
        /// </summary>
        /// <param name="input">Mã xác thực 6 số.</param>
        /// <returns>Trạng thái cài đặt sau khi bật.</returns>
        public virtual async Task<TwoFactorSetupDto> EnableAsync(VerifyTwoFactorDto input)
        {
            var user = await GetCurrentUserAsync();

            var pending = await _pendingSecrets.GetAsync(PendingKey(user.Id));
            if (pending == null || string.IsNullOrWhiteSpace(pending.Secret))
            {
                throw new Domain.Exceptions.DomainException(
                    "Phiên cài đặt 2FA đã hết hạn. Vui lòng quét lại mã QR.");
            }

            if (!VerifyCode(pending.Secret, input.Code))
            {
                throw new Domain.Exceptions.DomainException("Mã xác thực 2FA không đúng. Vui lòng thử lại.");
            }

            user.TotpSecret = pending.Secret;
            user.TwoFactorEnabled = true;
            await _users.UpdateAsync(user, autoSave: true);
            await _pendingSecrets.RemoveAsync(PendingKey(user.Id));

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

        /// <summary>Key cache cho secret 2FA đang chờ xác thực của một admin.</summary>
        private static string PendingKey(Guid userId) => $"2fa-pending:{userId:D}";
    }

    /// <summary>
    /// Secret 2FA đang chờ admin xác thực mã 6 số (được tạo ở
    /// <see cref="TwoFactorAppService.GetSetupAsync"/>, lưu server-side thay vì
    /// tin secret client gửi lên). Tự hết hạn theo TTL của cache.
    /// </summary>
    public class TwoFactorPendingSecretCacheItem
    {
        /// <summary>Secret base32 đang chờ xác thực.</summary>
        public string Secret { get; set; }
    }
}
