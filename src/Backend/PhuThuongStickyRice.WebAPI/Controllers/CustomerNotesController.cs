using PhuThuongStickyRice.Application.Wave3;
using PhuThuongStickyRice.Application.Wave3.DTOs;
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
    /// <summary>Item 89: CRM notes per customer phone (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerNotesController : ControllerBase
    {
        private readonly ICustomerNoteAppService _notes;

        public CustomerNotesController(ICustomerNoteAppService notes)
        {
            _notes = notes;
        }

        [HttpGet]
        public async Task<ActionResult> GetByPhone(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _notes.GetByPhoneAsync(phone));
        }

        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateCustomerNoteDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _notes.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _notes.DeleteAsync(id);
            return NoContent();
        }
    }
}
