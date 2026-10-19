using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// Forgot-password flow for customers via SMS OTP. No database table —
    /// OTPs and rate-limit counters live in <see cref="IMemoryCache"/>.
    /// <see cref="RequestOtpAsync"/> never reveals whether the phone number
    /// is registered (always "succeeds") to prevent account enumeration.
    /// </summary>
    public partial class PasswordResetOtpService
    {
        // .NET 7+: regex sinh ở compile-time — khởi động nhanh hơn
        // RegexOptions.Compiled (không tốn JIT codegen lúc chạy), pattern
        // và semantics giữ nguyên.
        [GeneratedRegex(@"^(0)(3|5|7|8|9)\d{8}$")]
        private static partial Regex VnPhoneRegex();

        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);
        private const int MaxOtpRequestsPerHour = 3;
        private const int MaxOtpAttempts = 3;
        private const int MinPasswordLength = 6;

        // Khi nhập sai OTP: giữ nguyên hạn dùng gốc của OTP nhưng tối thiểu
        // chừng này để user còn kịp nhập lại thay vì OTP hết hạn ngay.
        private static readonly TimeSpan MinFailedAttemptTtl = TimeSpan.FromSeconds(30);

        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _cache;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<PasswordResetOtpService> _logger;

        public PasswordResetOtpService(
            UserManager<User> userManager,
            IMemoryCache cache,
            ISmsSender smsSender,
            ILogger<PasswordResetOtpService> logger)
        {
            _userManager = userManager;
            _cache = cache;
            _smsSender = smsSender;
            _logger = logger;
        }

        /// <summary>
        /// Issues a 6-digit OTP for the phone number when the account exists.
        /// Always completes without error — callers must return a generic
        /// success message regardless of the outcome.
        /// </summary>
        public async Task RequestOtpAsync(string phone, CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            if (!VnPhoneRegex().IsMatch(phone))
            {
                return;
            }

            // Rate limit: max 3 OTP requests per phone per hour.
            var rateKey = $"pwdreset:ratelimit:{phone}";
            var count = _cache.TryGetValue<int>(rateKey, out var c) ? c : 0;
            if (count >= MaxOtpRequestsPerHour)
            {
                _logger.LogWarning("Quên mật khẩu: SĐT {Phone} vượt quá giới hạn yêu cầu OTP.", phone);
                return;
            }
            _cache.Set(rateKey, count + 1, RateLimitWindow);

            var user = await _userManager.FindByNameAsync(phone);
            if (user == null)
            {
                // Do not reveal that the phone is not registered.
                return;
            }

            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var entry = new OtpEntry
            {
                Code = otp,
                FailedAttempts = 0,
                ExpiresAt = DateTimeOffset.UtcNow.Add(OtpTtl)
            };
            _cache.Set(OtpCacheKey(phone), entry, OtpTtl);

            await _smsSender.SendAsync(
                phone,
                $"Mã OTP đặt lại mật khẩu Xôi Phú Thượng: {otp} (hiệu lực 5 phút).",
                cancellationToken);
        }

        /// <summary>
        /// Verifies the OTP and sets the new password. Returns a result with
        /// a Vietnamese error message when it fails.
        /// </summary>
        public async Task<PasswordResetResult> ResetPasswordAsync(
            string phone,
            string otp,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            otp = (otp ?? string.Empty).Trim();

            if (!VnPhoneRegex().IsMatch(phone))
            {
                return PasswordResetResult.Fail("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
            {
                return PasswordResetResult.Fail("Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            var key = OtpCacheKey(phone);
            if (!_cache.TryGetValue<OtpEntry>(key, out var entry) || entry == null)
            {
                return PasswordResetResult.Fail("Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.");
            }

            if (entry.FailedAttempts >= MaxOtpAttempts)
            {
                _cache.Remove(key);
                return PasswordResetResult.Fail("Bạn đã nhập sai OTP quá 3 lần. Vui lòng yêu cầu mã OTP mới.");
            }

            if (!string.Equals(entry.Code, otp, StringComparison.Ordinal))
            {
                return RegisterFailedOtpAttempt(key, entry);
            }

            var user = await _userManager.FindByNameAsync(phone);
            if (user == null)
            {
                _cache.Remove(key);
                return PasswordResetResult.Fail("Không tìm thấy tài khoản.");
            }

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return PasswordResetResult.Fail("Đặt lại mật khẩu thất bại. Vui lòng thử lại.");
            }

            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
            {
                _logger.LogWarning("Đặt lại mật khẩu thất bại cho SĐT {Phone}: {Errors}",
                    phone, string.Join("; ", addResult.Errors.Select(e => e.Description)));
                return PasswordResetResult.Fail("Đặt lại mật khẩu thất bại. Vui lòng thử lại.");
            }

            await _userManager.UpdateSecurityStampAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);
            _cache.Remove(key);

            _logger.LogInformation("Đặt lại mật khẩu thành công cho SĐT {Phone}.", phone);
            return PasswordResetResult.Ok();
        }

        /// <summary>
        /// Xử lý 1 lần nhập sai OTP: tăng số lần thử, giữ nguyên hạn dùng gốc
        /// của OTP (nhưng tối thiểu <see cref="MinFailedAttemptTtl"/>), trả về
        /// thông báo lỗi tương ứng số lần thử còn lại.
        /// </summary>
        private PasswordResetResult RegisterFailedOtpAttempt(string key, OtpEntry entry)
        {
            entry.FailedAttempts++;
            // Preserve the original expiry on failed attempts.
            var remaining = entry.ExpiresAt - DateTimeOffset.UtcNow;
            if (remaining < MinFailedAttemptTtl)
            {
                remaining = MinFailedAttemptTtl;
            }
            _cache.Set(key, entry, remaining);

            var left = MaxOtpAttempts - entry.FailedAttempts;
            return left > 0
                ? PasswordResetResult.Fail($"Mã OTP không đúng. Bạn còn {left} lần thử.")
                : PasswordResetResult.Fail("Bạn đã nhập sai OTP quá 3 lần. Vui lòng yêu cầu mã OTP mới.");
        }

        private static string OtpCacheKey(string phone) => $"pwdreset:{phone}";

        private sealed class OtpEntry
        {
            public string Code { get; set; }

            public int FailedAttempts { get; set; }

            public DateTimeOffset ExpiresAt { get; set; }
        }
    }

    public sealed class PasswordResetResult
    {
        public bool Succeeded { get; init; }

        public string ErrorMessage { get; init; }

        public static PasswordResetResult Ok() => new PasswordResetResult { Succeeded = true };

        public static PasswordResetResult Fail(string message) =>
            new PasswordResetResult { Succeeded = false, ErrorMessage = message };
    }
}
