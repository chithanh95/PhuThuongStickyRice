using PhuThuongStickyRice.Application.Catalog;
using PhuThuongStickyRice.Application.Products.DTOs;
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
    // Restored 2026-10-16: the dead-code sweep removed this controller as
    // "unreferenced", but the product add/edit form (AddEditProduct) manages
    // size options through these endpoints. Route fixed to kebab-case to match
    // the frontend (CatalogAdminService calls api/product-size-options/*).
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/product-size-options")]
    [ApiController]
    /// <summary>
    /// API quản lý tùy chọn kích cỡ sản phẩm (size S/M/L, khẩu phần to/nhỏ...).
    /// Form thêm/sửa sản phẩm (AddEditProduct) gọi các endpoint này.
    /// Route dạng kebab-case cho khớp frontend (CatalogAdminService gọi
    /// api/product-size-options/*). Chỉ Admin được gọi.
    /// </summary>
    public class ProductSizeOptionsController : ControllerBase
    {
        private readonly IProductSizeOptionAppService _appService;

        public ProductSizeOptionsController(IProductSizeOptionAppService appService)
        {
            _appService = appService;
        }

        /// <summary>
        /// Liệt kê các tùy chọn kích cỡ của một sản phẩm.
        /// </summary>
        /// <param name="productId">Id sản phẩm.</param>
        [HttpGet("by-product/{productId:guid}")]
        public async Task<ActionResult> GetByProduct(Guid productId, CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetByProductAsync(productId));
        }

        /// <summary>
        /// Thêm tùy chọn kích cỡ mới cho sản phẩm.
        /// Lỗi nghiệp vụ (DomainException, vd tên trùng) trả 400 với message thân thiện.
        /// </summary>
        /// <param name="dto">Tên size, giá chênh lệch và thứ tự hiển thị.</param>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateUpdateProductSizeOptionDto dto,
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

        /// <summary>
        /// Cập nhật tùy chọn kích cỡ.
        /// Lỗi nghiệp vụ (DomainException) trả 400 với message thân thiện.
        /// </summary>
        /// <param name="id">Id tùy chọn kích cỡ.</param>
        /// <param name="dto">Thông tin mới của tùy chọn.</param>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateUpdateProductSizeOptionDto dto,
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
        /// Xóa tùy chọn kích cỡ.
        /// </summary>
        /// <param name="id">Id tùy chọn kích cỡ cần xóa.</param>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _appService.DeleteAsync(id);
            return Ok();
        }
    }
}
