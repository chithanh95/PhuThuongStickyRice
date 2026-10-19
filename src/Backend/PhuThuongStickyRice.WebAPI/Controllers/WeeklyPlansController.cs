using PhuThuongStickyRice.Application.WeeklyPlans;
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
    /// Gói "Ăn sáng cả tuần" (item 185).
    /// Admin tạo/quản lý gói; khách mua và tự bấm "Đã nhận" mỗi ngày.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class WeeklyPlansController : ControllerBase
    {
        private readonly IWeeklyPlanAppService _weeklyPlans;

        public WeeklyPlansController(IWeeklyPlanAppService weeklyPlans)
        {
            _weeklyPlans = weeklyPlans;
        }

        /// <summary>Public: gói tuần đang bán.</summary>
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult> GetActive(CancellationToken cancellationToken = default)
        {
            return Ok(await _weeklyPlans.GetActiveAsync());
        }

        /// <summary>Admin: danh sách gói (mới nhất trước).</summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _weeklyPlans.GetListAsync());
        }

        /// <summary>Admin: tạo gói tuần mới.</summary>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateWeeklyPlanDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _weeklyPlans.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Public: khách mua gói.</summary>
        [AllowAnonymous]
        [HttpPost("buy")]
        public async Task<ActionResult> Buy(
            [FromBody] BuyWeeklyPlanDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _weeklyPlans.BuyAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Public: đơn mua gói theo SĐT.</summary>
        [AllowAnonymous]
        [HttpGet("my-orders")]
        public async Task<ActionResult> GetMyOrders(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _weeklyPlans.GetMyOrdersAsync(phone));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Public: khách tự bấm "Đã nhận" cho 1 ngày.</summary>
        [AllowAnonymous]
        [HttpPost("orders/{id:guid}/claim")]
        public async Task<ActionResult> Claim(
            Guid id,
            [FromBody] ClaimDayInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _weeklyPlans.MarkClaimedAsync(id, input?.Day));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class ClaimDayInput
        {
            /// <summary>Mon..Fri.</summary>
            public string Day { get; set; }
        }
    }
}
