using PhuThuongStickyRice.Application.Faq;
using PhuThuongStickyRice.Application.Faq.DTOs;
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
    /// Câu hỏi thường gặp (FAQ) của trang landing. Public chỉ đọc các mục đang
    /// bật (hỗ trợ tìm kiếm theo từ khóa); Admin quản lý toàn bộ nội dung.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FaqsController : ControllerBase
    {
        private readonly IFaqAppService _faqAppService;

        public FaqsController(IFaqAppService faqAppService)
        {
            _faqAppService = faqAppService;
        }

        /// <summary>Public: các FAQ đang bật (màn /faq), lọc theo từ khóa nếu có.</summary>
        /// <param name="searchTerm">Từ khóa tìm trong câu hỏi/câu trả lời (có thể bỏ trống).</param>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetPublic(
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.GetPublicAsync(searchTerm));
        }

        /// <summary>Admin: toàn bộ FAQ (kể cả mục đã tắt) cho màn hình quản lý.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.GetListAsync());
        }

        /// <summary>Admin: thêm 1 câu hỏi-câu trả lời mới.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateFaqItemDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.CreateAsync(dto));
        }

        /// <summary>Admin: cập nhật nội dung FAQ.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateFaqItemDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.UpdateAsync(id, dto));
        }

        /// <summary>Admin: xóa FAQ (trả 204 khi thành công).</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _faqAppService.DeleteAsync(id);
            return NoContent();
        }
    }
}
