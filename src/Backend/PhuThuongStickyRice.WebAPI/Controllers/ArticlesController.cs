

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Content;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 93: CMS bài viết tin tức cho trang landing. Admin CRUD toàn quyền;
    /// public chỉ đọc bài đã xuất bản (<c>api/Articles/published</c>). List trả
    /// excerpt (đoạn trích) nên form sửa phải gọi riêng GetById để lấy đủ Body.
    /// Lỗi nghiệp vụ (DomainException) được dịch thành BadRequest.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly IArticleAppService _articles;

        public ArticlesController(IArticleAppService articles)
        {
            _articles = articles;
        }

        /// <summary>
        /// Admin: danh sách bài viết (chỉ excerpt, không kèm nội dung đầy đủ).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _articles.GetListAsync());
        }

        /// <summary>
        /// Admin: 1 bài viết đầy đủ nội dung (form sửa). List chỉ trả excerpt
        /// Body nên edit phải gọi riêng endpoint này — xem ArticleAppService.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            return Ok(await _articles.GetByIdAsync(id));
        }

        /// <summary>
        /// Admin: tạo bài viết mới (tiêu đề, nội dung, trạng thái xuất bản).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateArticleDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _articles.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: cập nhật bài viết (nội dung, trạng thái xuất bản).
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateArticleDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _articles.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: xóa bài viết (trả 204 khi thành công).
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _articles.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Public: các bài viết đã xuất bản cho trang landing.</summary>
        [AllowAnonymous]
        [HttpGet("published")]
        public async Task<ActionResult> GetPublished(CancellationToken cancellationToken = default)
        {
            return Ok(await _articles.GetPublishedAsync());
        }
    }
}
