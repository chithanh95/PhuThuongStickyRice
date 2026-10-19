using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuThuongStickyRice.Application.Shop;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Đồng bộ giỏ hàng lên server cho khách đã đăng nhập (Item 137):
    /// khi đổi thiết bị/trình duyệt, giỏ hàng được khôi phục từ server.
    /// </summary>
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : AbpController
    {
        private readonly ICartAppService _cartAppService;

        /// <summary>Khởi tạo controller đồng bộ giỏ hàng.</summary>
        /// <param name="cartAppService">AppService đọc/ghi giỏ hàng của khách hiện tại.</param>
        public CartController(ICartAppService cartAppService)
        {
            _cartAppService = cartAppService;
        }

        /// <summary>
        /// Lấy giỏ hàng đã lưu của khách hiện tại (trả về JSON các dòng giỏ).
        /// </summary>
        /// <returns>JSON chứa <c>ItemsJson</c> — chuỗi JSON các dòng giỏ hàng.</returns>
        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {
            var itemsJson = await _cartAppService.GetAsync();
            return Ok(new { ItemsJson = itemsJson });
        }

        /// <summary>
        /// Lưu đè toàn bộ giỏ hàng hiện tại của khách lên server.
        /// </summary>
        /// <param name="model">Body chứa <c>ItemsJson</c> — JSON các dòng giỏ cần lưu.</param>
        [HttpPut]
        public async Task<ActionResult> Save([FromBody] SaveCartRequestModel model)
        {
            // Ghi đè bằng null vẫn hợp lệ: AppService xử lý như giỏ trống.
            await _cartAppService.SaveAsync(model?.ItemsJson);
            return Ok();
        }
    }

    /// <summary>
    /// Request body cho API lưu giỏ hàng (PUT /api/cart).
    /// </summary>
    public sealed class SaveCartRequestModel
    {
        /// <summary>Chuỗi JSON chứa các dòng giỏ hàng cần lưu.</summary>
        public string ItemsJson { get; set; }
    }
}
