using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Shop.DTOs;
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
    public class ShopSettingsController : ControllerBase
    {
        private readonly IShopSettingAppService _shopSettingAppService;

        public ShopSettingsController(IShopSettingAppService shopSettingAppService)
        {
            _shopSettingAppService = shopSettingAppService;
        }

        /// <summary>Public — the checkout page needs the fee rules.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Get(CancellationToken cancellationToken = default)
        {
            return Ok(await _shopSettingAppService.GetAsync());
        }

        /// <summary>
        /// Public storefront view (item 117): same settings but a full-day
        /// holiday today automatically pauses ordering with the holiday
        /// reason. The landing/shop/checkout pages use this endpoint.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<ActionResult> GetPublic(CancellationToken cancellationToken = default)
        {
            return Ok(await _shopSettingAppService.GetPublicAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<ActionResult> Update(
            [FromBody] ShopSettingDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _shopSettingAppService.UpdateAsync(dto));
        }
    }
}
