using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
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
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountsController : ControllerBase
    {
        private readonly IDiscountAppService _discountAppService;

        public DiscountsController(IDiscountAppService discountAppService)
        {
            _discountAppService = discountAppService;
        }

        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _discountAppService.GetListAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            return Ok(await _discountAppService.GetAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateDiscountCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _discountAppService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateDiscountCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _discountAppService.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _discountAppService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Public: validates a code against a cart subtotal.</summary>
        [AllowAnonymous]
        [HttpPost("validate")]
        public async Task<ActionResult> Validate(
            [FromBody] ValidateDiscountInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _discountAppService.ValidateAsync(input));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
