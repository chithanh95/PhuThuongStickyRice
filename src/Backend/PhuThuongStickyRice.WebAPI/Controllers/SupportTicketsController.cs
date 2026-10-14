using PhuThuongStickyRice.Application.Support;
using PhuThuongStickyRice.Application.Support.DTOs;
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
    public class SupportTicketsController : ControllerBase
    {
        private readonly ISupportTicketAppService _ticketAppService;

        public SupportTicketsController(ISupportTicketAppService ticketAppService)
        {
            _ticketAppService = ticketAppService;
        }

        /// <summary>Public: customer opens a support ticket (/ho-tro).</summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateSupportTicketDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _ticketAppService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] string status,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _ticketAppService.GetListAsync(status));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/reply")]
        public async Task<ActionResult> Reply(
            Guid id,
            [FromBody] ReplySupportTicketDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _ticketAppService.ReplyAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
