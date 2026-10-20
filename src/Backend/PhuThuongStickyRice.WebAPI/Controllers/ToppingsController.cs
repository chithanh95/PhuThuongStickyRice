using PhuThuongStickyRice.Application.Catalog;
using PhuThuongStickyRice.Application.Catalog.DTOs;
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
    /// <summary>
    /// API quản trị topping (món ăn kèm thêm vào xôi: hành phi, ruốc...).
    /// Mặc định yêu cầu quyền Admin; riêng endpoint <c>active</c> cho phép
    /// ẩn danh để trang shop hiển thị danh sách topping đang bán.
    /// </summary>
    public class ToppingsController : ControllerBase
    {
        private readonly IToppingAppService _appService;

        public ToppingsController(IToppingAppService appService)
        {
            _appService = appService;
        }

        /// <summary>Lấy các topping đang bán cho trang shop (không cần đăng nhập).</summary>
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult> GetActive(CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetActiveAsync());
        }

        /// <summary>Lấy toàn bộ topping cho trang quản trị (kể cả topping đã ẩn).</summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetListAsync());
        }

        /// <summary>Lấy chi tiết 1 topping theo id.</summary>
        /// <param name="id">Id của topping.</param>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetAsync(id));
        }

        /// <summary>Tạo topping mới.</summary>
        /// <param name="dto">Tên, giá và trạng thái của topping.</param>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateUpdateToppingDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                // Lỗi nghiệp vụ (tên trùng, giá không hợp lệ...): trả 400 kèm
                // thông báo thân thiện thay vì để thành lỗi 500.
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Cập nhật thông tin topping.</summary>
        /// <param name="id">Id của topping cần sửa.</param>
        /// <param name="dto">Thông tin mới của topping.</param>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateUpdateToppingDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                // Lỗi nghiệp vụ (tên trùng, giá không hợp lệ...): trả 400 kèm
                // thông báo thân thiện thay vì để thành lỗi 500.
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Xóa topping — topping đang gắn với món ăn sẽ bị chặn ở tầng AppService.</summary>
        /// <param name="id">Id của topping cần xóa.</param>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _appService.DeleteAsync(id);
            return Ok();
        }
    }
}
