using PhuThuongStickyRice.Application.Shop;
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
    public class PrepaidCardsController : ControllerBase
    {
        private readonly IPrepaidCardAppService _prepaidCardAppService;

        public PrepaidCardsController(IPrepaidCardAppService prepaidCardAppService)
        {
            _prepaidCardAppService = prepaidCardAppService;
        }

        /// <summary>Public: customer buys a prepaid card package.</summary>
        [AllowAnonymous]
        [HttpPost("buy")]
        public async Task<ActionResult> Buy(
            [FromBody] BuyPrepaidCardDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _prepaidCardAppService.BuyAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Public: list cards of a phone number.</summary>
        [AllowAnonymous]
        [HttpGet("my")]
        public async Task<ActionResult> GetMyCards(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _prepaidCardAppService.GetMyCardsAsync(phone));
        }

        /// <summary>Admin: paged card list, search by name/phone/code.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] string searchTerm,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _prepaidCardAppService.GetListAsync(searchTerm, skip, take));
        }

        /// <summary>Admin: redeem one suit from a card code.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{code}/redeem")]
        public async Task<ActionResult> Redeem(
            string code,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _prepaidCardAppService.RedeemAsync(code));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
