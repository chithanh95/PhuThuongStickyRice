

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Discounts;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 91: "giờ vàng" promotions (Admin CRUD, public active preview).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionsController : ControllerBase
    {
        private readonly IPromotionAppService _promotions;

        public PromotionsController(IPromotionAppService promotions)
        {
            _promotions = promotions;
        }

        /// <summary>Danh sách tất cả khuyến mãi "giờ vàng" (Admin).</summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _promotions.GetListAsync());
        }

        /// <summary>
        /// Tạo khung giờ khuyến mãi mới (khung giờ tính theo giờ Việt Nam,
        /// hỗ trợ khung qua đêm như 22:00–02:00). Lỗi validation trả 400
        /// với message hiển thị trực tiếp cho admin.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreatePromotionDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _promotions.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Sửa khung giờ khuyến mãi (giờ Việt Nam). Lỗi validation trả 400.</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreatePromotionDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _promotions.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Xóa khung giờ khuyến mãi. Trả 204 khi thành công.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _promotions.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Public: the best currently-active promotion (Vietnam time).</summary>
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult> GetActive(CancellationToken cancellationToken = default)
        {
            var promo = await _promotions.GetBestActiveAsync();
            return promo == null ? (ActionResult)NoContent() : Ok(promo);
        }
    }
}
