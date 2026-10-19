using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
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
    /// API mã giảm giá (admin): CRUD mã, kiểm tra mã có áp được không
    /// (validate), liệt kê mã đang chạy công khai và chọn mã tốt nhất cho
    /// giỏ hàng (best — checkout tự gợi ý).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DiscountsController : ControllerBase
    {
        private readonly IDiscountAppService _discountAppService;

        public DiscountsController(IDiscountAppService discountAppService)
        {
            _discountAppService = discountAppService;
        }

        /// <summary>
        /// Admin: danh sách mã giảm giá (500 mã mới nhất — bảng phình to vì
        /// mỗi lượt quay/sinh nhật sinh 1 mã riêng nên không lấy hết).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _discountAppService.GetListAsync());
        }

        /// <summary>Admin: chi tiết 1 mã giảm giá theo Id.</summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            return Ok(await _discountAppService.GetAsync(id));
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): tạo mã giảm giá mới. Mã được
        /// chuẩn hóa in hoa và kiểm tra không trùng; lỗi nghiệp vụ (trùng mã,
        /// thiếu giá trị giảm) trả 400 với message tiếng Việt cho admin.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateDiscountCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _discountAppService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): cập nhật mã giảm giá. Kiểm tra
        /// trùng mã loại trừ chính nó nên sửa field khác không bị báo trùng oan.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateDiscountCodeDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _discountAppService.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin (cần quyền discounts.manage): xóa mã giảm giá. Đơn cũ giữ
        /// nguyên số tiền đã giảm (DiscountAmount lưu snapshot trên đơn,
        /// không join lại bảng mã).
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _discountAppService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Public: kiểm tra mã giảm giá có áp được cho giỏ hàng không (còn hạn,
        /// đủ giá trị đơn tối thiểu, chưa hết lượt dùng, đúng SĐT chủ mã).
        /// Checkout gọi trước khi áp mã để báo lỗi ngay cho khách.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("validate")]
        public async Task<ActionResult> Validate(
            [FromBody] ValidateDiscountInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _discountAppService.ValidateAsync(input, input.CustomerPhone));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Item 139 (Public): danh sách mã giảm giá công khai đang chạy để
        /// checkout tự gợi ý/áp mã tốt nhất (kết hợp endpoint best bên dưới).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("active")]
        public async Task<ActionResult> GetActivePublic(CancellationToken cancellationToken = default)
        {
            return Ok(await _discountAppService.GetActivePublicAsync());
        }

        /// <summary>
        /// perf-hunt 2026-10-19 (Public): best money-off code for the cart in
        /// one round-trip — replaces the old client loop of
        /// GET active + N x POST validate on the checkout hot path.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("best")]
        public async Task<ActionResult> EvaluateBest(
            [FromBody] EvaluateBestDiscountInput input,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _discountAppService.EvaluateBestDiscountAsync(input));
        }
    }
}
