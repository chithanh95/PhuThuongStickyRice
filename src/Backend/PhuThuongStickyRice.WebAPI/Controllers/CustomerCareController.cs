using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ICustomerTagAppService _tags;
        private readonly IVipPriorityAppService _vip;

        public CustomerCareController(
            ICustomerProfileAppService profiles,
            ICustomerTagAppService tags,
            IVipPriorityAppService vip)
        {
            _profiles = profiles;
            _tags = tags;
            _vip = vip;
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

        /// <summary>Admin/Cashier: tags for a customer phone number.</summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("tags")]
        public async Task<ActionResult<List<CustomerTagDto>>> GetTags(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _tags.GetTagsAsync(phone));
        }

        /// <summary>Admin/Cashier: add a tag to a customer phone.</summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("tags")]
        public async Task<ActionResult<CustomerTagDto>> AddTag(
            [FromBody] CreateCustomerTagDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _tags.AddTagAsync(dto));
            }
            catch (DbUpdateException)
            {
                // bug-hunt 2026-10-19 (run 22:35): race — 2 request đồng thời
                // (2 tab admin, bấm trùng nhanh) cùng qua bước kiểm tra trùng
                // trong AddTagAsync rồi cùng insert; unique (CustomerPhone,
                // TagName) chỉ cho 1 bên qua, bên thua nhận DbUpdateException
                // lúc UoW commit → 500. Bên thua lấy lại tag bên thắng vừa tạo
                // để "thêm tag" idempotent thay vì lỗi.
                var tags = await _tags.GetTagsAsync(dto?.CustomerPhone);
                var existing = tags.FirstOrDefault(t =>
                    string.Equals(t.TagName, dto?.TagName?.Trim(),
                        StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    return Ok(existing);
                }
                throw;
            }
        }

        /// <summary>Admin/Cashier: remove a customer tag.</summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpDelete("tags/{id:guid}")]
        public async Task<ActionResult> DeleteTag(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _tags.DeleteTagAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Item 130 (đúng): Admin/Cashier: danh sách SĐT đang được đánh dấu VIP.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("vip")]
        public async Task<ActionResult<List<CustomerTagDto>>> GetVips(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _vip.GetVipsAsync());
        }

        /// <summary>
        /// Item 130 (đúng): Admin/Cashier: đánh dấu một SĐT là VIP (ưu tiên).
        /// Idempotent — đánh dấu 2 lần không lỗi.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("vip/{phone}")]
        public async Task<ActionResult<CustomerTagDto>> MarkVip(
            string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _vip.MarkVipAsync(phone));
        }

        /// <summary>
        /// Item 130 (đúng): Admin/Cashier: gỡ đánh dấu VIP khỏi một SĐT.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpDelete("vip/{phone}")]
        public async Task<ActionResult> UnmarkVip(
            string phone,
            CancellationToken cancellationToken = default)
        {
            await _vip.UnmarkVipAsync(phone);
            return NoContent();
        }

        /// <summary>
        /// Admin/Cashier: customer list as .xlsx — same filters as the admin
        /// customer page (searchTerm, tag, inactiveOnly).
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("customers/export")]
        public async Task<ActionResult> ExportCustomers(
            [FromQuery] string searchTerm = null,
            [FromQuery] string tag = null,
            [FromQuery] bool inactiveOnly = false,
            CancellationToken cancellationToken = default)
        {
            var bytes = await _tags.ExportCustomersAsync(searchTerm, tag, inactiveOnly);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"khach-hang-{VietnamTime.ToVietnamTime(DateTime.UtcNow):yyyy-MM-dd}.xlsx");
        }
    }
}
