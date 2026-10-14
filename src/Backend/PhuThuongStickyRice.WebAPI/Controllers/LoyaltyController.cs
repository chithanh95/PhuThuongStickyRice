using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Loyalty.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        public LoyaltyController(ILoyaltyAppService loyaltyAppService)
        {
            _loyaltyAppService = loyaltyAppService;
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
    }
}
