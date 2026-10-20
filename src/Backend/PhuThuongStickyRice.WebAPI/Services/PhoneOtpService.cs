using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Services
{
    /// <summary>
    /// OTP theo SĐT dùng chung cho các thao tác nhạy cảm của khách không
    /// đăng nhập (shop dùng SĐT làm định danh): xóa địa chỉ, đổi ngày sinh,
    /// xem chi tiết lịch sử đơn, xác nhận chi tiêu ví/điểm ở checkout.
    /// Cùng chuẩn với <see cref="PasswordResetOtpService"/>: mã 6 số, hiệu
    /// lực 5 phút, tối đa 3 lần nhập sai, tối đa 3 lần xin mã/giờ cho mỗi
    /// (SĐT, mục đích), mã dùng 1 lần (verify đúng là hủy ngay). OTP lưu
    /// <see cref="IMemoryCache"/> — không cần bảng DB/migration.
    /// Khác quên mật khẩu: service này KHÔNG kiểm tra tài khoản tồn tại —
    /// OTP ở đây chứng minh "người thao tác đang cầm SĐT" (sổ địa chỉ/lịch
    /// sử đơn tồn tại độc lập với tài khoản đăng nhập).
    /// </summary>
    public partial class PhoneOtpService
    {
        // .NET 7+: regex sinh ở compile-time, cùng pattern SĐT VN với
        // PasswordResetOtpService.
        [GeneratedRegex(@"^(0)(3|5|7|8|9)\d{8}$")]
        private static partial Regex VnPhoneRegex();

        private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);
        private const int MaxOtpRequestsPerHour = 3;
        private const int MaxOtpAttempts = 3;

        // Khi nhập sai OTP: giữ nguyên hạn dùng gốc nhưng tối thiểu chừng
        // này để khách còn kịp nhập lại (giống PasswordResetOtpService).
        private static readonly TimeSpan MinFailedAttemptTtl = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Các mục đích OTP được phép — mỗi mục đích 1 "kênh" OTP riêng:
        /// mã xin cho mục đích này không dùng được cho mục đích khác.
        /// </summary>
        public static class Purposes
        {
            /// <summary>Xóa 1 địa chỉ trong sổ địa chỉ theo SĐT.</summary>
            public const string AddressDelete = "address-delete";

            /// <summary>Đổi ngày sinh của tài khoản loyalty theo SĐT.</summary>
            public const string BirthdayChange = "birthday-change";

            /// <summary>Xem chi tiết đầy đủ lịch sử đơn hàng theo SĐT.</summary>
            public const string OrderHistory = "order-history";

            /// <summary>Xác nhận dùng ví/điểm của SĐT khi thanh toán (lô checkout dùng sau).</summary>
            public const string CheckoutSpend = "checkout-spend";
        }

        // Nhãn tiếng Việt theo mục đích — đưa vào nội dung SMS để khách
        // biết mã này dùng cho việc gì, tránh bị lừa đọc mã cho kẻ gian.
        private static readonly IReadOnlyDictionary<string, string> PurposeLabels =
            new Dictionary<string, string>
            {
                [Purposes.AddressDelete] = "xóa địa chỉ đã lưu",
                [Purposes.BirthdayChange] = "đổi ngày sinh",
                [Purposes.OrderHistory] = "xem chi tiết lịch sử đơn hàng",
                [Purposes.CheckoutSpend] = "xác nhận thanh toán bằng ví/điểm",
            };

        private readonly IMemoryCache _cache;
        private readonly ISmsSender _smsSender;
        private readonly ILogger<PhoneOtpService> _logger;

        /// <summary>Khởi tạo service OTP theo SĐT với cache trong bộ nhớ và kênh gửi SMS.</summary>
        public PhoneOtpService(
            IMemoryCache cache,
            ISmsSender smsSender,
            ILogger<PhoneOtpService> logger)
        {
            _cache = cache;
            _smsSender = smsSender;
            _logger = logger;
        }

        /// <summary>
        /// Sinh mã OTP 6 số cho (SĐT, mục đích) và gửi qua SMS. Trả về kết
        /// quả thất bại kèm thông báo tiếng Việt khi SĐT sai định dạng, mục
        /// đích không hợp lệ hoặc vượt quá 3 lần xin mã/giờ.
        /// </summary>
        /// <param name="phone">SĐT nhận OTP (định dạng VN 10 số bắt đầu bằng 0).</param>
        /// <param name="purpose">Một trong các hằng <see cref="Purposes"/>.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        /// <returns>Kết quả gửi OTP.</returns>
        public async Task<PhoneOtpResult> SendAsync(
            string phone, string purpose, CancellationToken cancellationToken = default)
        {
            phone = (phone ?? string.Empty).Trim();
            purpose = (purpose ?? string.Empty).Trim();

            if (!PurposeLabels.TryGetValue(purpose, out var label))
            {
                return PhoneOtpResult.Fail("Mục đích xác thực không hợp lệ.");
            }

            if (!VnPhoneRegex().IsMatch(phone))
            {
                return PhoneOtpResult.Fail("Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            // Rate limit theo (SĐT, mục đích): 3 lần/giờ — chặn spam SMS và
            // dò mã bằng cách xin mã liên tục để reset số lần thử.
            var rateKey = $"phoneotp:ratelimit:{purpose}:{phone}";
            var count = _cache.TryGetValue<int>(rateKey, out var cachedCount) ? cachedCount : 0;
            if (count >= MaxOtpRequestsPerHour)
            {
                LogOtpRateLimited(_logger, phone, purpose);
                return PhoneOtpResult.Fail("Bạn đã yêu cầu mã OTP quá nhiều lần. Vui lòng thử lại sau 1 giờ.");
            }
            _cache.Set(rateKey, count + 1, RateLimitWindow);

            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
            var entry = new OtpEntry
            {
                Code = otp,
                FailedAttempts = 0,
                ExpiresAt = DateTimeOffset.UtcNow.Add(OtpTtl)
            };
            _cache.Set(OtpCacheKey(phone, purpose), entry, OtpTtl);

            await _smsSender.SendAsync(
                phone,
                $"Mã OTP {label} Xôi Phú Thượng: {otp} (hiệu lực 5 phút). Không chia sẻ mã này cho bất kỳ ai.",
                cancellationToken);

            return PhoneOtpResult.Ok();
        }

        /// <summary>
        /// Kiểm tra mã OTP cho (SĐT, mục đích). Mã đúng bị hủy ngay (dùng 1
        /// lần); nhập sai tăng số lần thử — quá 3 lần mã bị vô hiệu. Trả về
        /// kết quả kèm thông báo tiếng Việt khi thất bại.
        /// </summary>
        /// <param name="phone">SĐT đã xin mã.</param>
        /// <param name="purpose">Mục đích đã xin mã (phải khớp lúc gửi).</param>
        /// <param name="code">Mã 6 số khách nhập.</param>
        /// <returns>Kết quả xác thực OTP.</returns>
        public Task<PhoneOtpResult> VerifyAsync(string phone, string purpose, string code)
        {
            phone = (phone ?? string.Empty).Trim();
            purpose = (purpose ?? string.Empty).Trim();
            code = (code ?? string.Empty).Trim();

            if (!PurposeLabels.ContainsKey(purpose) || string.IsNullOrEmpty(code))
            {
                return Task.FromResult(PhoneOtpResult.Fail(
                    "Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới."));
            }

            var key = OtpCacheKey(phone, purpose);
            if (!_cache.TryGetValue<OtpEntry>(key, out var entry) || entry == null)
            {
                return Task.FromResult(PhoneOtpResult.Fail(
                    "Mã OTP không đúng hoặc đã hết hạn. Vui lòng yêu cầu mã mới."));
            }

            if (entry.FailedAttempts >= MaxOtpAttempts)
            {
                _cache.Remove(key);
                return Task.FromResult(PhoneOtpResult.Fail(
                    "Bạn đã nhập sai OTP quá 3 lần. Vui lòng yêu cầu mã OTP mới."));
            }

            if (!string.Equals(entry.Code, code, StringComparison.Ordinal))
            {
                return Task.FromResult(RegisterFailedAttempt(key, entry));
            }

            // Mã dùng 1 lần: hủy ngay sau khi verify thành công để không
            // thể dùng lại cho thao tác khác.
            _cache.Remove(key);
            return Task.FromResult(PhoneOtpResult.Ok());
        }

        /// <summary>
        /// Ghi nhận 1 lần nhập sai OTP: tăng số lần thử, giữ nguyên hạn dùng
        /// gốc (tối thiểu <see cref="MinFailedAttemptTtl"/>), trả về thông
        /// báo lỗi kèm số lần thử còn lại.
        /// </summary>
        private PhoneOtpResult RegisterFailedAttempt(string key, OtpEntry entry)
        {
            entry.FailedAttempts++;
            var remaining = entry.ExpiresAt - DateTimeOffset.UtcNow;
            if (remaining < MinFailedAttemptTtl)
            {
                remaining = MinFailedAttemptTtl;
            }
            _cache.Set(key, entry, remaining);

            var left = MaxOtpAttempts - entry.FailedAttempts;
            return left > 0
                ? PhoneOtpResult.Fail($"Mã OTP không đúng. Bạn còn {left} lần thử.")
                : PhoneOtpResult.Fail("Bạn đã nhập sai OTP quá 3 lần. Vui lòng yêu cầu mã OTP mới.");
        }

        private static string OtpCacheKey(string phone, string purpose) => $"phoneotp:{purpose}:{phone}";

        private sealed class OtpEntry
        {
            /// <summary>Mã OTP 6 chữ số đã gửi qua SMS.</summary>
            public string Code { get; set; }

            /// <summary>Số lần nhập sai tính đến hiện tại; quá 3 lần thì mã bị vô hiệu.</summary>
            public int FailedAttempts { get; set; }

            /// <summary>Hạn dùng gốc của mã — giữ nguyên qua các lần nhập sai.</summary>
            public DateTimeOffset ExpiresAt { get; set; }
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated
        // (cùng pattern với PasswordResetOtpService).

        /// <summary>Log khi (SĐT, mục đích) vượt quá giới hạn xin OTP (3 lần/giờ).</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "PhoneOtp: SĐT {Phone} vượt quá giới hạn yêu cầu OTP cho mục đích {Purpose}.")]
        private static partial void LogOtpRateLimited(ILogger logger, string phone, string purpose);
    }

    /// <summary>Kết quả gửi/xác thực OTP theo SĐT: thành công hoặc thất bại kèm thông báo lỗi tiếng Việt.</summary>
    public sealed class PhoneOtpResult
    {
        /// <summary>true khi thao tác OTP thành công.</summary>
        public bool Succeeded { get; init; }

        /// <summary>Thông báo lỗi hiển thị cho khách (tiếng Việt); null khi thành công.</summary>
        public string ErrorMessage { get; init; }

        /// <summary>Tạo kết quả thành công.</summary>
        public static PhoneOtpResult Ok() => new PhoneOtpResult { Succeeded = true };

        /// <summary>Tạo kết quả thất bại với thông báo lỗi cho khách.</summary>
        public static PhoneOtpResult Fail(string message) =>
            new PhoneOtpResult { Succeeded = false, ErrorMessage = message };
    }
}
