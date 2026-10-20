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
    /// API cài đặt cửa hàng: bản public (shop/landing/checkout dùng) và bản
    /// đầy đủ cho admin (gồm cả secret tích hợp). Admin sửa ở trang
    /// /admin/cai-dat — các trang shop đọc lại bản public để cập nhật
    /// giờ bán, thông tin chuyển khoản, webhook...
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
        /// Admin lưu toàn bộ cài đặt cửa hàng từ form /admin/cai-dat.
        /// Ghi đè toàn bộ cấu hình — client phải gửi đủ các trường.
        /// </summary>
        /// <param name="dto">Toàn bộ cài đặt cửa hàng.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<ActionResult> Update(
            [FromBody] ShopSettingDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _shopSettingAppService.UpdateAsync(dto));
        }

        /// <summary>
        /// Body request gửi thử webhook đơn hàng mới.
        /// </summary>
        public class TestOrderWebhookRequest
        {
            /// <summary>
            /// URL ghi đè. Để trống = dùng URL webhook đã lưu trong cài đặt.
            /// </summary>
            public string Url { get; set; }
        }

        /// <summary>
        /// Item 180: nút "Gửi thử" ở trang cài đặt — post một payload đơn hàng
        /// mẫu tới URL webhook đã cấu hình (hoặc URL ghi đè), timeout 5 giây.
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
