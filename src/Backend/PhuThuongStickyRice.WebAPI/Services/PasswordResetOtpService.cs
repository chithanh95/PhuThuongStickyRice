using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PhuThuongStickyRice.Domain.Entities;
using System;
using System.Globalization;
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

        /// <summary>Khởi tạo service quên mật khẩu qua SMS OTP.</summary>
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
            var count = _cache.TryGetValue<int>(rateKey, out var cachedCount) ? cachedCount : 0;
            if (count >= MaxOtpRequestsPerHour)
            {
                LogOtpRateLimited(_logger, phone);
                return;
            }
            _cache.Set(rateKey, count + 1, RateLimitWindow);

            var user = await _userManager.FindByNameAsync(phone);
            if (user == null)
            {
                // Do not reveal that the phone is not registered.
                return;
            }

            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
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

            var inputFailure = ValidateResetInputs(phone, newPassword);
            if (inputFailure != null)
            {
                return inputFailure;
            }

            var key = OtpCacheKey(phone);
            if (!_cache.TryGetValue<OtpEntry>(key, out var entry) || entry == null)
            {
                return PasswordResetResult.Fail("Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới.");
            }

            var otpFailure = ValidateOtpEntry(key, otp, entry);
            if (otpFailure != null)
            {
                return otpFailure;
            }

            var user = await _userManager.FindByNameAsync(phone);
            if (user == null)
            {
                _cache.Remove(key);
                return PasswordResetResult.Fail("Không tìm thấy tài khoản.");
            }

            var setFailure = await SetNewPasswordAsync(user, newPassword, phone);
            if (setFailure != null)
            {
                return setFailure;
            }

            // Kẻ gian biết OTP đã đổi được mật khẩu — đổi security stamp để mọi
            // phiên đăng nhập trước đó (của chính kẻ gian hay của chủ tài
            // khoản) đều bị vô hiệu ngay lập tức.
            await _userManager.UpdateSecurityStampAsync(user);
            // Mở lại tài khoản nếu trước đó bị khóa do nhập sai mật khẩu quá
            // nhiều lần — đặt lại mật khẩu qua OTP là xác thực thành công.
            await _userManager.ResetAccessFailedCountAsync(user);
            // OTP chỉ dùng 1 lần: xóa ngay sau khi dùng xong để chặn nhập lại.
            _cache.Remove(key);

            LogResetSucceeded(_logger, phone);
            return PasswordResetResult.Ok();
        }

        /// <summary>
        /// Kiểm tra định dạng SĐT và độ dài mật khẩu mới của yêu cầu đặt lại
        /// mật khẩu. Trả về kết quả thất bại kèm thông báo lỗi khi đầu vào
        /// không hợp lệ, null khi hợp lệ.
        /// </summary>
        private static PasswordResetResult ValidateResetInputs(string phone, string newPassword)
        {
            if (!VnPhoneRegex().IsMatch(phone))
            {
                return PasswordResetResult.Fail("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < MinPasswordLength)
            {
                return PasswordResetResult.Fail("Mật khẩu mới phải có ít nhất 6 ký tự.");
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra trạng thái OTP trong cache: chưa quá số lần thử và mã OTP
        /// nhập khớp. Trả về kết quả thất bại kèm thông báo lỗi khi kiểm tra
        /// không đạt (lần nhập sai được ghi nhận qua
        /// <see cref="RegisterFailedOtpAttempt"/>), null khi đạt.
        /// </summary>
        private PasswordResetResult ValidateOtpEntry(string key, string otp, OtpEntry entry)
        {
            if (entry.FailedAttempts >= MaxOtpAttempts)
            {
                _cache.Remove(key);
                return PasswordResetResult.Fail("Bạn đã nhập sai OTP quá 3 lần. Vui lòng yêu cầu mã OTP mới.");
            }

            if (!string.Equals(entry.Code, otp, StringComparison.Ordinal))
            {
                return RegisterFailedOtpAttempt(key, entry);
            }

            return null;
        }

        /// <summary>
        /// Đặt mật khẩu mới cho tài khoản qua <see cref="UserManager{TUser}"/>.
        /// Trả về kết quả thất bại kèm thông báo lỗi khi không đặt được,
        /// null khi thành công.
        /// </summary>
        private async Task<PasswordResetResult> SetNewPasswordAsync(User user, string newPassword, string phone)
        {
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return PasswordResetResult.Fail("Đặt lại mật khẩu thất bại. Vui lòng thử lại.");
            }

            var addResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addResult.Succeeded)
            {
                LogResetFailed(_logger, phone, string.Join("; ", addResult.Errors.Select(e => e.Description)));
                return PasswordResetResult.Fail("Đặt lại mật khẩu thất bại. Vui lòng thử lại.");
            }

            return null;
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
            /// <summary>Mã OTP 6 chữ số đã gửi qua SMS.</summary>
            public string Code { get; set; }

            /// <summary>Số lần nhập sai OTP tính đến hiện tại; quá 3 lần thì OTP bị vô hiệu.</summary>
            public int FailedAttempts { get; set; }

            /// <summary>Hạn dùng gốc của OTP — giữ nguyên qua các lần nhập sai.</summary>
            public DateTimeOffset ExpiresAt { get; set; }
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated
        // (tránh parse message template và boxing mỗi lần gọi) — cùng pattern
        // với StaffActivityLogger, hành vi log giữ nguyên.

        /// <summary>Log khi SĐT vượt quá giới hạn yêu cầu OTP (3 lần/giờ).</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "Quên mật khẩu: SĐT {Phone} vượt quá giới hạn yêu cầu OTP.")]
        private static partial void LogOtpRateLimited(ILogger logger, string phone);

        /// <summary>Log khi UserManager.AddPasswordAsync thất bại khi đặt lại mật khẩu.</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "Đặt lại mật khẩu thất bại cho SĐT {Phone}: {Errors}")]
        private static partial void LogResetFailed(ILogger logger, string phone, string errors);

        /// <summary>Log khi đặt lại mật khẩu qua OTP thành công.</summary>
        [LoggerMessage(Level = LogLevel.Information, Message = "Đặt lại mật khẩu thành công cho SĐT {Phone}.")]
        private static partial void LogResetSucceeded(ILogger logger, string phone);
    }

    /// <summary>Kết quả đặt lại mật khẩu: thành công hoặc thất bại kèm thông báo lỗi tiếng Việt.</summary>
    public sealed class PasswordResetResult
    {
        /// <summary>true khi đặt lại mật khẩu thành công.</summary>
        public bool Succeeded { get; init; }

        /// <summary>Thông báo lỗi hiển thị cho khách (tiếng Việt); null khi thành công.</summary>
        public string ErrorMessage { get; init; }

        /// <summary>Tạo kết quả thành công.</summary>
        public static PasswordResetResult Ok() => new PasswordResetResult { Succeeded = true };

        /// <summary>Tạo kết quả thất bại với thông báo lỗi cho khách.</summary>
        public static PasswordResetResult Fail(string message) =>
            new PasswordResetResult { Succeeded = false, ErrorMessage = message };
    }
}
