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
    /// <summary>
    /// API cài đặt quán (api/shop-settings): admin đọc/ghi toàn bộ cài đặt
    /// (kể cả secret tích hợp như webhook/MoMo); bản public rút gọn cho
    /// landing/shop/checkout — ngày nghỉ lễ full-day tự tạm dừng đặt hàng.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/shop-settings")]
    [ApiController]
    public class ShopSettingsController : ControllerBase
    {
        private readonly IShopSettingAppService _shopSettingAppService;

        public ShopSettingsController(IShopSettingAppService shopSettingAppService)
        {
            _shopSettingAppService = shopSettingAppService;
        }

        /// <summary>
        /// Admin — full settings including integration secrets (the settings
        /// form). Anonymous storefront pages must use GET api/shop-settings/public.
        /// </summary>
        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Admin: cập nhật cài đặt quán (tên, hotline, giờ mở cửa, phí ship,
        /// quy tắc điểm loyalty, tích hợp webhook...).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<ActionResult> Update(
            [FromBody] ShopSettingDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _shopSettingAppService.UpdateAsync(dto));
        }

        public class TestOrderWebhookRequest
        {
            /// <summary>
            /// Optional override URL. Empty = use the saved setting.
            /// </summary>
            public string Url { get; set; }
        }

        /// <summary>
        /// Item 180: "Gửi thử" — posts a sample new-order payload to the
        /// configured webhook URL (or the override) with a 5s timeout.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("test-order-webhook")]
        public async Task<ActionResult> TestOrderWebhook(
            [FromBody] TestOrderWebhookRequest request,
            CancellationToken cancellationToken = default)
        {
            var (ok, message) = await _shopSettingAppService.TestNewOrderWebhookAsync(request?.Url);
            return Ok(new { isSuccess = ok, message });
        }
    }
}
