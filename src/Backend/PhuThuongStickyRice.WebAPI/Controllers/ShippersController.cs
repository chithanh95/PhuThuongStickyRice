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
    /// <summary>Item 82: delivery shippers (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ShippersController : ControllerBase
    {
        private readonly IShipperAppService _shippers;

        public ShippersController(IShipperAppService shippers)
        {
            _shippers = shippers;
        }

        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] bool activeOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _shippers.GetListAsync(activeOnly));
        }

        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateShipperDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _shippers.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] UpdateShipperDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _shippers.UpdateAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _shippers.DeleteAsync(id);
            return NoContent();
        }
    }
}
