using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Loyalty.DTOs;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API điểm thành viên: khách tra cứu điểm/hạng theo SĐT, tự cập nhật
    /// ngày sinh; admin liệt kê tài khoản, cộng/trừ điểm tay, xem sinh nhật
    /// sắp tới và phát voucher sinh nhật.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public partial class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyAppService _loyaltyAppService;
        private readonly IDiscountAppService _discountAppService;
        private readonly PhoneOtpService _phoneOtpService;
        private readonly ILogger<LoyaltyController> _logger;

        // Quà sinh nhật: prefix mã + mệnh giá (VND) + thời hạn dùng của
        // voucher do admin sinh hàng loạt — dùng chung quy ước với
        // endpoint public landing birthday-gift (SN/20000/30 ngày).
        private const string BirthdayVoucherPrefix = "SN";
        private const decimal BirthdayVoucherAmount = 20000;
        private const int BirthdayVoucherValidityDays = 30;

        /// <summary>
        /// Khởi tạo controller điểm thành viên. <paramref name="loyaltyAppService"/>
        /// xử lý điểm/hạng/ngày sinh; <paramref name="discountAppService"/>
        /// dùng để sinh voucher quà sinh nhật hàng loạt;
        /// <paramref name="phoneOtpService"/> xác thực OTP cho thao tác nhạy
        /// cảm theo SĐT (đổi ngày sinh).
        /// </summary>
        public LoyaltyController(
            ILoyaltyAppService loyaltyAppService,
            IDiscountAppService discountAppService,
            PhoneOtpService phoneOtpService,
            ILogger<LoyaltyController> logger)
        {
            _loyaltyAppService = loyaltyAppService;
            _discountAppService = discountAppService;
            _phoneOtpService = phoneOtpService;
            _logger = logger;
        }

        /// <summary>
        /// Public: tóm tắt loyalty theo SĐT (trả 204 khi SĐT chưa có tài
        /// khoản). Bảo mật (audit 2026-10-20): caller ẨN DANH chỉ nhận bản
        /// TỐI THIỂU (hạng + tên khách) — trước đây chỉ cần biết SĐT người
        /// khác là đọc được điểm, tổng chi tiêu, ngày sinh, mã giới thiệu
        /// của họ. Bản đầy đủ chỉ trả khi người gọi đã đăng nhập bằng đúng
        /// SĐT đang tra (JWT username của khách chính là SĐT).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("by-phone")]
        public async Task<ActionResult> GetByPhone(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            var account = await _loyaltyAppService.GetByPhoneAsync(phone);
            if (account == null)
            {
                return NoContent();
            }

            if (IsOwnerRequest(phone))
            {
                return Ok(account);
            }

            var summary = await _loyaltyAppService.GetPublicSummaryByPhoneAsync(phone);
            return Ok(summary);
        }

        /// <summary>
        /// Public: kiểm tra mã giới thiệu NGAY tại checkout để báo lỗi trên
        /// màn hình, thay vì để tới lúc tạo đơn server mới ném exception
        /// (user báo 2026-10-21). Chỉ trả trạng thái hợp lệ + thông báo —
        /// không lộ thông tin chủ mã. Server vẫn validate lại khi tạo đơn
        /// (<c>OrderAppService.ValidateReferralAsync</c>) nên endpoint này
        /// chỉ là lớp UX, không phải cửa an ninh duy nhất.
        /// </summary>
        /// <param name="code">Mã giới thiệu khách nhập ở checkout.</param>
        /// <param name="phone">SĐT người mua — để chặn dùng mã của chính mình.</param>
        [AllowAnonymous]
        [HttpGet("referral-check")]
        public async Task<ActionResult> CheckReferralCode(
            [FromQuery] string code,
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return Ok(new { valid = false, message = "Vui lòng nhập mã giới thiệu." });
            }

            var referrer = await _loyaltyAppService.FindByReferralCodeAsync(code);
            if (referrer == null)
            {
                return Ok(new { valid = false, message = "Mã giới thiệu không tồn tại." });
            }

            if (!string.IsNullOrWhiteSpace(phone)
                && PhoneNumber.PhonesMatch(referrer.Phone, phone))
            {
                return Ok(new { valid = false, message = "Không thể dùng mã giới thiệu của chính mình." });
            }

            return Ok(new { valid = true, message = "Mã giới thiệu hợp lệ." });
        }

        /// <summary>
        /// Kiểm tra request hiện tại có phải của CHÍNH chủ SĐT đang tra
        /// không: đã đăng nhập và JWT username (SĐT của khách) khớp SĐT
        /// truyền vào (so khớp khoan dung định dạng qua
        /// <see cref="PhoneNumber.PhonesMatch"/>). Nhân viên đăng nhập bằng
        /// email nên không bao giờ khớp — họ dùng các endpoint admin.
        /// </summary>
        /// <param name="phone">SĐT đang được tra cứu.</param>
        /// <returns>true khi người gọi là chủ của SĐT đó.</returns>
        private bool IsOwnerRequest(string phone)
        {
            if (User?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            var identityDigits = PhoneNumber.DigitsOnly(User.Identity.Name);
            return identityDigits.Length > 0
                && PhoneNumber.PhonesMatch(identityDigits, phone);
        }

        /// <summary>
        /// Public: khách tự cập nhật ngày sinh theo SĐT. Bắt buộc OTP gửi
        /// về chính SĐT đó (purpose birthday-change, xin mã qua
        /// POST api/phone-otp/send) — trước đây chỉ cần biết SĐT là đổi
        /// được ngày sinh người khác (phá quà/voucher sinh nhật của họ).
        /// Thiếu/sai mã trả 400 kèm thông báo tiếng Việt.
        /// </summary>
        [AllowAnonymous]
        [HttpPut("by-phone/birthday")]
        public async Task<ActionResult> UpdateBirthday(
            [FromQuery] string phone,
            [FromQuery] string otpCode,
            [FromBody] UpdateBirthdayInput input,
            CancellationToken cancellationToken = default)
        {
            var verify = await _phoneOtpService.VerifyAsync(
                phone, PhoneOtpService.Purposes.BirthdayChange, otpCode);
            if (!verify.Succeeded)
            {
                return BadRequest(new { message = verify.ErrorMessage });
            }

            try
            {
                return Ok(await _loyaltyAppService.UpdateBirthdayAsync(phone, input));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Admin: danh sách tài khoản điểm thành viên (200 tài khoản chi tiêu
        /// cao nhất, tìm kiếm theo SĐT) — màn quản lý loyalty ở admin.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _loyaltyAppService.GetListAsync(searchTerm));
        }

        /// <summary>
        /// Admin: cộng/trừ điểm thủ công cho 1 SĐT (thưởng bù, phạt gian lận).
        /// Điểm không được về âm; lỗi nghiệp vụ trả 400.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{phone}/adjust")]
        public async Task<ActionResult> Adjust(
            string phone,
            [FromBody] AdjustPointsInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _loyaltyAppService.AdjustAsync(phone, input));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Admin: tài khoản có sinh nhật trong N ngày tới (mặc định 7, tối đa
        /// 60) — dùng để chuẩn bị chiến dịch gửi voucher sinh nhật.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("birthdays")]
        public async Task<ActionResult> GetUpcomingBirthdays(
            [FromQuery] int daysAhead = 7,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _loyaltyAppService.GetUpcomingBirthdaysAsync(daysAhead));
        }

        /// <summary>
        /// Item 132: tạo voucher sinh nhật cho khách có sinh nhật trong 7 ngày tới.
        /// Mỗi khách 1 mã prefix "SN", giảm 20k, HSD 30 ngày.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("birthdays/vouchers")]
        public async Task<ActionResult> GenerateBirthdayVouchers(
            [FromQuery] int daysAhead = 7,
            CancellationToken cancellationToken = default)
        {
            // bug-hunt 2026-10-20: race check-then-act — tập phonesWithCode
            // được load 1 lần trước vòng lặp; admin bấm nút 2 lần (hoặc 2 tab)
            // thì request sau load tập check trước khi request trước commit
            // xong → sinh 2 mã SN/khách. Serialize cả batch theo prefix để
            // request sau thấy đủ mã request trước vừa sinh.
            return await KeyedAsyncLock.RunAsync<ActionResult>(
                "birthday-vouchers:SN", async () =>
                {
                    var birthdays = await _loyaltyAppService.GetUpcomingBirthdaysAsync(daysAhead);
                    var created = new List<object>();

                    // perf-hunt 2026-10-19: kiểm tra mã SN còn dùng được cho TẤT CẢ
                    // khách trong 1 query (IN clause) thay vì gọi HasUsableCodeAsync
                    // từng khách (N+1). Điều kiện lọc giữ nguyên nên kết quả tương
                    // đương từng phần tử.
                    var phonesWithCode = await _discountAppService.GetPhonesWithUsableCodeAsync(
                        BirthdayVoucherPrefix, birthdays.Select(b => b.Phone));

                    foreach (var b in birthdays)
                    {
                        await TryCreateBirthdayVoucherAsync(b, phonesWithCode, created);
                    }

                    return Ok(new { Created = created.Count, Vouchers = created });
                });
        }

        /// <summary>
        /// Sinh 1 voucher sinh nhật cho khách: bỏ qua nếu khách đã có mã SN còn
        /// dùng được. Vòng lặp sinh hàng loạt: 1 khách lỗi (trùng mã, lỗi DB...)
        /// không được làm sập cả batch — nhưng không được nuốt im lặng, phải
        /// ghi log kèm SĐT để admin biết khách nào bị bỏ qua và đối chiếu sau.
        /// </summary>
        private async Task TryCreateBirthdayVoucherAsync(
            LoyaltyAccountDto birthday, HashSet<string> phonesWithCode, List<object> created)
        {
            try
            {
                // Mỗi khách chỉ 1 mã SN còn dùng được — không có check
                // này, admin bấm nút 2 lần sẽ sinh 2 mã/khách.
                if (phonesWithCode.Contains(birthday.Phone))
                {
                    return;
                }
                var voucher = await _discountAppService.CreateGeneratedAsync(
                    new CreateGeneratedDiscountDto
                    {
                        Prefix = BirthdayVoucherPrefix,
                        DiscountAmount = BirthdayVoucherAmount,
                        ExpiresAt = DateTime.UtcNow.AddDays(BirthdayVoucherValidityDays),
                        OwnerPhone = birthday.Phone
                    });
                created.Add(new { Phone = birthday.Phone, Code = voucher.Code });
            }
            catch (Exception ex)
            {
                LogBirthdayVoucherFailed(_logger, ex, birthday.Phone);
            }
        }

        // CA1848: logging hiệu năng cao bằng LoggerMessage source-generated
        // (tránh cấp phát params array + boxing mỗi lần log).

        /// <summary>Log khi tạo voucher quà sinh nhật hàng loạt bị lỗi cho 1 SĐT.</summary>
        [LoggerMessage(Level = LogLevel.Warning, Message = "Tạo voucher sinh nhật thất bại cho SĐT {Phone}.")]
        private static partial void LogBirthdayVoucherFailed(ILogger logger, Exception ex, string phone);
    }
}
