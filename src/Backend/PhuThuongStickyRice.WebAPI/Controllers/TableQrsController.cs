

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 95: table QR codes (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TableQrsController : ControllerBase
    {
        private readonly ITableQrAppService _tableQrs;

        public TableQrsController(ITableQrAppService tableQrs)
        {
            _tableQrs = tableQrs;
        }

        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _tableQrs.GetListAsync());
        }

        /// <summary>Generates (or refreshes) the QR for a table label.</summary>
        [HttpPost]
        public async Task<ActionResult> Generate(
            [FromBody] CreateTableQrDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _tableQrs.GenerateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Downloads the saved QR as a PNG file. Anonymous: QR codes are
        /// printed on tables — the PNG only encodes the public shop URL.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}/png")]
        public async Task<ActionResult> DownloadPng(Guid id, CancellationToken cancellationToken = default)
        {
            var png = await _tableQrs.RenderPngAsync(id);
            return File(png, "image/png");
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _tableQrs.DeleteAsync(id);
            return NoContent();
        }
    }
}
