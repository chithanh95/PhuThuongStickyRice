using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Loyalty.DTOs;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
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

        public LoyaltyController(
            ILoyaltyAppService loyaltyAppService,
            IDiscountAppService discountAppService)
        {
            _loyaltyAppService = loyaltyAppService;
            _discountAppService = discountAppService;
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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _loyaltyAppService.GetListAsync(searchTerm));
        }

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

            foreach (var b in birthdays)
            {
                try
                {
                    // Mỗi khách chỉ 1 mã SN còn dùng được — cùng pattern dedup
                    // như BirthdayVoucherWorker (HasUsableCodeAsync "BDAY").
                    // Không có check này, admin bấm nút 2 lần sẽ sinh 2 mã/khách.
                    if (await _discountAppService.HasUsableCodeAsync("SN", b.Phone))
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
                catch (Exception)
                {
                    // Bỏ qua khách lỗi, tiếp tục khách khác.
                }
            }

            return Ok(new { Created = created.Count, Vouchers = created });
        }
    }
}
