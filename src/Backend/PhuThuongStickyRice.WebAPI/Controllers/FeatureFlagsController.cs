

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
        /// <summary>Feature flags (item 105): public enabled-map for UI gating + admin toggles.</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [Route("api/v1/[controller]")]
        [ApiController]
        public class FeatureFlagsController : ControllerBase
        {
            private readonly IFeatureFlagAppService _flags;

            public FeatureFlagsController(IFeatureFlagAppService flags)
            {
                _flags = flags;
            }

            /// <summary>Public: key → enabled map used by the storefront to gate UI.</summary>
            [AllowAnonymous]
            [HttpGet]
            public async Task<ActionResult<Dictionary<string, bool>>> GetEnabledMap(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _flags.GetEnabledMapAsync());
            }

            /// <summary>Admin: all flags with metadata.</summary>
            [Authorize(Roles = "Admin")]
            [HttpGet("admin")]
            public async Task<ActionResult<List<FeatureFlagDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _flags.GetListAsync());
            }

            /// <summary>Admin: flip a flag.</summary>
            [Authorize(Roles = "Admin")]
            [HttpPut("{key}")]
            public async Task<ActionResult<FeatureFlagDto>> SetEnabled(
                string key, [FromBody] SetFeatureFlagEnabledDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _flags.SetEnabledAsync(key, dto.Enabled));
            }
        }
}
