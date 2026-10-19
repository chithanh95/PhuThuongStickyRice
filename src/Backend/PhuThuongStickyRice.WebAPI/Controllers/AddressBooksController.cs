

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Customer;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Address book (item 119): public, keyed by customer phone.</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class AddressBooksController : ControllerBase
        {
            private readonly IAddressBookAppService _addresses;

            public AddressBooksController(IAddressBookAppService addresses)
            {
                _addresses = addresses;
            }

            [AllowAnonymous]
            [HttpGet]
            public async Task<ActionResult<List<AddressBookDto>>> GetByPhone(
                [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _addresses.GetByPhoneAsync(phone));
            }

            [AllowAnonymous]
            [HttpPost]
            public async Task<ActionResult<AddressBookDto>> Save(
                [FromBody] SaveAddressDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _addresses.SaveAsync(dto));
            }

            [AllowAnonymous]
            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id, [FromQuery] string phone,
                CancellationToken cancellationToken = default)
            {
                await _addresses.DeleteAsync(id, phone);
                return Ok();
            }
        }
}
