using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Loyalty.DTOs;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
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
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyAppService _loyaltyAppService;
        private readonly IDiscountAppService _discountAppService;
        private readonly ILogger<LoyaltyController> _logger;

        public LoyaltyController(
            ILoyaltyAppService loyaltyAppService,
            IDiscountAppService discountAppService,
            ILogger<LoyaltyController> logger)
        {
            _loyaltyAppService = loyaltyAppService;
            _discountAppService = discountAppService;
            _logger = logger;
        }

        /// <summary>Public: loyalty summary for a phone number.</summary>
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

            return Ok(account);
        }

        /// <summary>Public: customer sets their own birthday.</summary>
        [AllowAnonymous]
        [HttpPut("by-phone/birthday")]
        public async Task<ActionResult> UpdateBirthday(
            [FromQuery] string phone,
            [FromBody] UpdateBirthdayInput input,
            CancellationToken cancellationToken = default)
        {
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
            var birthdays = await _loyaltyAppService.GetUpcomingBirthdaysAsync(daysAhead);
            var created = new List<object>();

            // perf-hunt 2026-10-19: kiểm tra mã SN còn dùng được cho TẤT CẢ
            // khách trong 1 query (IN clause) thay vì gọi HasUsableCodeAsync
            // từng khách (N+1). Điều kiện lọc giữ nguyên nên kết quả tương
            // đương từng phần tử.
            var phonesWithCode = await _discountAppService.GetPhonesWithUsableCodeAsync(
                "SN", birthdays.Select(b => b.Phone));

            foreach (var b in birthdays)
            {
                try
                {
                    // Mỗi khách chỉ 1 mã SN còn dùng được — không có check
                    // này, admin bấm nút 2 lần sẽ sinh 2 mã/khách.
                    if (phonesWithCode.Contains(b.Phone))
                    {
                        continue;
                    }
                    var voucher = await _discountAppService.CreateGeneratedAsync(
                        new CreateGeneratedDiscountDto
                        {
                            Prefix = "SN",
                            DiscountAmount = 20000,
                            ExpiresAt = DateTime.UtcNow.AddDays(30),
                            OwnerPhone = b.Phone
                        });
                    created.Add(new { Phone = b.Phone, Code = voucher.Code });
                }
                // Loop sinh voucher cho hàng loạt khách: 1 khách lỗi (trùng mã, lỗi DB...)
                // không được làm sập cả batch — nhưng không được nuốt im lặng, phải
                // ghi log kèm SĐT để admin biết khách nào bị bỏ qua và đối chiếu sau.
                catch (Exception ex)
                {
                    // Bỏ qua khách lỗi, tiếp tục khách khác.
                    _logger.LogWarning(ex, "Tạo voucher sinh nhật thất bại cho SĐT {Phone}.", b.Phone);
                }
            }

            return Ok(new { Created = created.Count, Vouchers = created });
        }
    }
}
