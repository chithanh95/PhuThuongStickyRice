

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Branches (item 109): public active list + admin CRUD.</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [Route("api/v1/[controller]")]
        [ApiController]
        public class BranchesController : ControllerBase
        {
            private readonly IBranchAppService _branches;

            public BranchesController(IBranchAppService branches)
            {
                _branches = branches;
            }

            /// <summary>Public: active branches for the shop branch picker.</summary>
            [AllowAnonymous]
            [HttpGet]
            public async Task<ActionResult<List<BranchDto>>> GetActive(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.GetActiveAsync());
            }

            [Authorize(Roles = "Admin")]
            [HttpGet("admin")]
            public async Task<ActionResult<List<BranchDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.GetListAsync());
            }

            [Authorize(Roles = "Admin")]
            [HttpGet("admin/{id}")]
            public async Task<ActionResult<BranchDto>> Get(Guid id,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.GetAsync(id));
            }

            [Authorize(Roles = "Admin")]
            [HttpPost("admin")]
            public async Task<ActionResult<BranchDto>> Create(
                [FromBody] CreateUpdateBranchDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.CreateAsync(dto));
            }

            [Authorize(Roles = "Admin")]
            [HttpPut("admin/{id}")]
            public async Task<ActionResult<BranchDto>> Update(
                Guid id, [FromBody] CreateUpdateBranchDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.UpdateAsync(id, dto));
            }

            [Authorize(Roles = "Admin")]
            [HttpDelete("admin/{id}")]
            public async Task<ActionResult> Delete(Guid id,
                CancellationToken cancellationToken = default)
            {
                await _branches.DeleteAsync(id);
                return Ok();
            }
        }
}
