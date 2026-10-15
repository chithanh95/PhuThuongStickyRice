using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Saved addresses of the authenticated customer. Every endpoint is
    /// owner-scoped server-side via the JWT identity.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/customer/addresses")]
    [ApiController]
    [Authorize]
    public class CustomerAddressesController : ControllerBase
    {
        private readonly ICustomerAddressAppService _addresses;

        public CustomerAddressesController(ICustomerAddressAppService addresses)
        {
            _addresses = addresses;
        }

        [HttpGet]
        public async Task<ActionResult<List<CustomerAddressDto>>> GetMine(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.GetMyAsync());
        }

        [HttpPost]
        public async Task<ActionResult<CustomerAddressDto>> Create(
            [FromBody] CreateCustomerAddressDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.CreateAsync(dto));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CustomerAddressDto>> Update(
            Guid id,
            [FromBody] UpdateCustomerAddressDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.UpdateAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _addresses.DeleteAsync(id);
            return Ok();
        }

        [HttpPost("{id}/default")]
        public async Task<ActionResult> SetDefault(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await _addresses.SetDefaultAsync(id);
            return Ok();
        }
    }
}
