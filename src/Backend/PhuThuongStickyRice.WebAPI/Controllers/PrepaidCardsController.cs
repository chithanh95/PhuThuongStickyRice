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
    // bug-hunt 2026-10-19: route kebab-case cho khớp frontend
    // (PrepaidCardService/PrepaidCardAdminService gọi api/prepaid-cards/*);
    // [controller] cho ra api/prepaidcards khiến toàn bộ UI mua/tra cứu/
    // quản lý thẻ trả trước bị 404.
    [Route("api/prepaid-cards")]
    [ApiController]
    /// <summary>
    /// API thẻ trả trước: khách mua thẻ (public), tra cứu thẻ theo SĐT (public),
    /// và nhân viên quản lý danh sách/đổi thẻ tại quán (chỉ Admin).
    /// </summary>
    public class PrepaidCardsController : ControllerBase
    {
        private readonly IPrepaidCardAppService _prepaidCardAppService;

        /// <summary>Khởi tạo controller thẻ trả trước.</summary>
        /// <param name="prepaidCardAppService">AppService xử lý nghiệp vụ thẻ trả trước.</param>
        public PrepaidCardsController(IPrepaidCardAppService prepaidCardAppService)
        {
            _prepaidCardAppService = prepaidCardAppService;
        }

        /// <summary>
        /// Khách mua một gói thẻ trả trước (không cần đăng nhập).
        /// Lỗi nghiệp vụ (DomainException) trả 400 với message thân thiện để hiển thị cho khách.
        /// </summary>
        /// <param name="dto">Gói thẻ muốn mua và thông tin người mua.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
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

        /// <summary>
        /// Khách tra cứu các thẻ trả trước của mình bằng số điện thoại (không cần đăng nhập).
        /// </summary>
        /// <param name="phone">Số điện thoại đã mua thẻ.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [AllowAnonymous]
        [HttpGet("my")]
        public async Task<ActionResult> GetMyCards(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _prepaidCardAppService.GetMyCardsAsync(phone));
        }

        /// <summary>
        /// Admin: danh sách thẻ trả trước phân trang, tìm theo tên/SĐT/mã thẻ.
        /// </summary>
        /// <param name="searchTerm">Chuỗi tìm kiếm (tên, SĐT hoặc mã thẻ).</param>
        /// <param name="skip">Số dòng bỏ qua (phân trang).</param>
        /// <param name="take">Số dòng lấy mỗi trang.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
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

        /// <summary>
        /// Admin: đổi (redeem) 1 lượt trên thẻ bằng mã thẻ — khi khách đến quán dùng thẻ.
        /// Lỗi nghiệp vụ (thẻ không tồn tại, hết lượt, đã khóa...) trả 400 với message thân thiện.
        /// </summary>
        /// <param name="code">Mã thẻ trả trước.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
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
