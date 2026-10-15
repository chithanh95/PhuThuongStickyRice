using PhuThuongStickyRice.Application.Customer;
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
    /// <summary>
    /// Customer-care admin endpoints: birthday list + gifts (item 92) and
    /// inactive-customer reminders (item 91, Zalo OA stub).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/admin/customer-care")]
    [ApiController]
    [Authorize]
    public class CustomerCareController : ControllerBase
    {
        private readonly ICustomerProfileAppService _profiles;

        public CustomerCareController(ICustomerProfileAppService profiles)
        {
            _profiles = profiles;
        }

        /// <summary>Admin: registered customers with a birthday in a month (1-12).</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("birthdays")]
        public async Task<ActionResult<List<BirthdayMemberDto>>> GetBirthdays(
            [FromQuery] int month,
            CancellationToken cancellationToken = default)
        {
            if (month == 0)
            {
                month = DateTime.UtcNow.Month;
            }

            return Ok(await _profiles.GetBirthdaysAsync(month));
        }

        /// <summary>Admin: grant this year's birthday gift to a member.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("birthdays/{userId:guid}/gift")]
        public async Task<ActionResult<BirthdayGiftResultDto>> GrantBirthdayGift(
            Guid userId,
            [FromBody] GrantBirthdayGiftDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profiles.GrantBirthdayGiftAsync(userId, dto));
        }

        /// <summary>Admin/Cashier: remind an inactive customer (Zalo OA stub).</summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("reminders")]
        public async Task<ActionResult<CustomerReminderDto>> CreateReminder(
            [FromBody] CreateReminderDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profiles.CreateReminderAsync(dto));
        }

        /// <summary>Admin/Cashier: reminder history for a phone number.</summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("reminders")]
        public async Task<ActionResult<List<CustomerReminderDto>>> GetReminders(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profiles.GetRemindersAsync(phone));
        }
    }
}
