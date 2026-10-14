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
