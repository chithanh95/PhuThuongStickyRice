using PhuThuongStickyRice.Application.Policies;
using PhuThuongStickyRice.Application.Policies.DTOs;
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
    /// Wave 6b (item 28): static policy pages rendered at /chinh-sach/{slug}.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    // Item 107: versioned prefix — both routes stay live.
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyAppService _policyAppService;

        public PoliciesController(IPolicyAppService policyAppService)
        {
            _policyAppService = policyAppService;
        }

        /// <summary>Public: slug + title of every policy page (footer links).</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetPublicList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _policyAppService.GetPublicListAsync());
        }

        /// <summary>Public: one policy page by slug.</summary>
        [AllowAnonymous]
        [HttpGet("{slug}")]
        public async Task<ActionResult> GetBySlug(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var page = await _policyAppService.GetPublicAsync(slug);
            if (page == null)
            {
                return NotFound("Không tìm thấy trang chính sách.");
            }

            return Ok(page);
        }

        /// <summary>Admin: all policy pages with bodies.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<ActionResult> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _policyAppService.GetListAsync());
        }

        /// <summary>Admin: edit a policy page (title/body/order).</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] UpdatePolicyPageDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _policyAppService.UpdateAsync(id, dto));
        }
    }
}
