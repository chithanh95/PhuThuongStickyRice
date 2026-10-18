

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Short links (item 116): admin CRUD + public create (quick-view "🔗 Link ngắn").</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class ShortLinksController : ControllerBase
        {
            private readonly IShortLinkAppService _links;

            public ShortLinksController(IShortLinkAppService links)
            {
                _links = links;
            }

            [Authorize(Roles = "Admin")]
            [HttpGet]
            public async Task<ActionResult<List<ShortLinkDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _links.GetListAsync());
            }

            [AllowAnonymous]
            [HttpPost]
            public async Task<ActionResult<ShortLinkDto>> Create(
                [FromBody] CreateShortLinkDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _links.CreateAsync(dto));
            }

            [Authorize(Roles = "Admin")]
            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id,
                CancellationToken cancellationToken = default)
            {
                await _links.DeleteAsync(id);
                return Ok();
            }
        }
}
