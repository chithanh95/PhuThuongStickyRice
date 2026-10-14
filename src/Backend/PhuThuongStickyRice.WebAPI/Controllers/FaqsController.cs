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

        /// <summary>Public: active FAQs (/faq), optional search.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetPublic(
            [FromQuery] string searchTerm,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.GetPublicAsync(searchTerm));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.GetListAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateFaqItemDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.CreateAsync(dto));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateFaqItemDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _faqAppService.UpdateAsync(id, dto));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _faqAppService.DeleteAsync(id);
            return NoContent();
        }
    }
}
