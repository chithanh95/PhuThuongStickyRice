

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

        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _rules.GetListAsync());
        }

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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _rules.DeleteAsync(id);
            return NoContent();
        }
    }
}
