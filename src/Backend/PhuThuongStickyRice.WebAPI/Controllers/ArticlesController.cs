

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Content;
using PhuThuongStickyRice.Application.Content;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 93: news CMS for the landing page (Admin CRUD, public published).</summary>
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

        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _articles.GetListAsync());
        }

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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _articles.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Public: published articles for the landing page.</summary>
        [AllowAnonymous]
        [HttpGet("published")]
        public async Task<ActionResult> GetPublished(CancellationToken cancellationToken = default)
        {
            return Ok(await _articles.GetPublishedAsync());
        }
    }
}
