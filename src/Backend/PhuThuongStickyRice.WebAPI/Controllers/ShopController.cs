using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Public storefront endpoints under <c>api/shop</c> (item 117:
    /// holiday schedule).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/shop")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly IHolidayScheduleAppService _holidays;

        public ShopController(IHolidayScheduleAppService holidays)
        {
            _holidays = holidays;
        }

        /// <summary>
        /// Public: upcoming holidays in the next 30 days (landing page
        /// "Lịch nghỉ sắp tới" section).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("holidays")]
        public async Task<ActionResult> GetUpcomingHolidays(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _holidays.GetUpcomingAsync());
        }

        /// <summary>Admin: lists all holidays, nearest first.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/holidays")]
        public async Task<ActionResult> GetHolidays(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _holidays.GetListAsync());
        }

        /// <summary>Admin: adds a holiday.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/holidays")]
        public async Task<ActionResult> CreateHoliday(
            [FromBody] CreateHolidayScheduleDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _holidays.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Admin: edits a holiday.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/holidays/{id:guid}")]
        public async Task<ActionResult> UpdateHoliday(
            Guid id,
            [FromBody] UpdateHolidayScheduleDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _holidays.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Admin: removes a holiday.</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/holidays/{id:guid}")]
        public async Task<ActionResult> DeleteHoliday(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _holidays.DeleteAsync(id);
            return Ok();
        }
    }
}
