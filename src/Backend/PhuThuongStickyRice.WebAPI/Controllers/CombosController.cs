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
    /// <summary>
    /// API combo món (admin): CRUD combo; 1 endpoint public cho shop lấy
    /// combo đang bán. Xóa combo không ảnh hưởng đơn cũ (đơn lưu snapshot
    /// giá/món, không join lại bảng combo).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CombosController : ControllerBase
    {
        private readonly IComboAppService _appService;

        public CombosController(IComboAppService appService)
        {
            _appService = appService;
        }

        /// <summary>
        /// Public: combo đang bán để hiển thị ở shop (đã lọc theo thời gian bán).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult> GetActive(CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetActiveAsync());
        }

        /// <summary>Admin: danh sách tất cả combo (kể cả combo đã ngưng bán).</summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetListAsync());
        }

        /// <summary>Admin: chi tiết 1 combo theo Id.</summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetAsync(id));
        }

        /// <summary>
        /// Admin: tạo combo món mới (nhóm món bán kèm + giá combo). Lỗi
        /// nghiệp vụ (trùng tên, danh sách món rỗng) trả 400 với message tiếng Việt.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateUpdateComboDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Admin: cập nhật combo món theo Id. Lỗi nghiệp vụ trả 400.</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateUpdateComboDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: xóa combo. Đơn cũ giữ nguyên giá đã bán (snapshot trên đơn,
        /// không join lại bảng combo).
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _appService.DeleteAsync(id);
            return Ok();
        }
    }
}
