

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Loyalty;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 92: automatic reward rules on order completion (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RewardRulesController : ControllerBase
    {
        private readonly IRewardRuleAppService _rules;

        public RewardRulesController(IRewardRuleAppService rules)
        {
            _rules = rules;
        }

        /// <summary>
        /// Admin: danh sách quy tắc thưởng tự động khi đơn hoàn thành
        /// (tặng điểm/voucher theo điều kiện đơn) — màn cấu hình loyalty ở admin.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _rules.GetListAsync());
        }

        /// <summary>
        /// Admin: tạo quy tắc thưởng tự động mới. Lỗi nghiệp vụ (điều kiện
        /// không hợp lệ) trả 400 với message tiếng Việt cho admin.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateOrderRewardRuleDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _rules.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: cập nhật quy tắc thưởng tự động theo Id. Lỗi nghiệp vụ
        /// trả 400 với message tiếng Việt.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateOrderRewardRuleDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _rules.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: xóa quy tắc thưởng tự động. Điểm/voucher đã thưởng trước
        /// đó không bị thu hồi (chỉ áp cho các đơn hoàn thành sau khi xóa).
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _rules.DeleteAsync(id);
            return NoContent();
        }
    }
}
