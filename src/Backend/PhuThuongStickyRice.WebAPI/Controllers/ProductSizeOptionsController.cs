using PhuThuongStickyRice.Application.Catalog;
using PhuThuongStickyRice.Application.Products.DTOs;
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
    // Restored 2026-10-16: the dead-code sweep removed this controller as
    // "unreferenced", but the product add/edit form (AddEditProduct) manages
    // size options through these endpoints. Route fixed to kebab-case to match
    // the frontend (CatalogAdminService calls api/product-size-options/*).
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/product-size-options")]
    [ApiController]
    public class ProductSizeOptionsController : ControllerBase
    {
        private readonly IProductSizeOptionAppService _appService;

        public ProductSizeOptionsController(IProductSizeOptionAppService appService)
        {
            _appService = appService;
        }

        [HttpGet("by-product/{productId:guid}")]
        public async Task<ActionResult> GetByProduct(Guid productId, CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetByProductAsync(productId));
        }

        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateUpdateProductSizeOptionDto dto,
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

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Update(
            Guid id,
            [FromBody] CreateUpdateProductSizeOptionDto dto,
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

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _appService.DeleteAsync(id);
            return Ok();
        }
    }
}
