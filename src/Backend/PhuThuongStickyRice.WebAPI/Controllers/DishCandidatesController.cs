using PhuThuongStickyRice.Application.Catalog;
using PhuThuongStickyRice.Application.Catalog.DTOs;
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
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class DishCandidatesController : ControllerBase
    {
        private readonly IDishCandidateAppService _appService;

        public DishCandidatesController(IDishCandidateAppService appService)
        {
            _appService = appService;
        }

        /// <summary>Public: candidate dishes ordered by votes.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GetActive(CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetActiveAsync());
        }

        /// <summary>Public: vote for a candidate dish.</summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/vote")]
        public async Task<ActionResult> Vote(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.VoteAsync(id));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<ActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetListAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("manage")]
        public async Task<ActionResult> Create(
            [FromBody] CreateUpdateDishCandidateDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("manage/{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateUpdateDishCandidateDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("manage/{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _appService.DeleteAsync(id);
            return Ok();
        }
    }
}
