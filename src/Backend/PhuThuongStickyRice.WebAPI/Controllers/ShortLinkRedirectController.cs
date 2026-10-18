

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

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Short-link redirect: GET /s/{code} → 302 to the target URL (item 116).</summary>
        [Produces("application/json")]
        [Route("s")]
        [ApiController]
        public class ShortLinkRedirectController : ControllerBase
        {
            private readonly IShortLinkAppService _links;

            public ShortLinkRedirectController(IShortLinkAppService links)
            {
                _links = links;
            }

            [AllowAnonymous]
            [HttpGet("{code}")]
            public async Task<ActionResult> RedirectToTarget(string code,
                CancellationToken cancellationToken = default)
            {
                var target = await _links.ResolveAsync(code);
                if (string.IsNullOrWhiteSpace(target))
                {
                    return NotFound("Liên kết không tồn tại.");
                }

                return Redirect(target);
            }
        }
}
