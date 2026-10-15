using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Logged-in customer's own profile: taste preferences (item 90) and
    /// birthday (item 92). Everything is owner-scoped via the JWT identity.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/customer/profile")]
    [ApiController]
    [Authorize]
    public class CustomerProfileController : ControllerBase
    {
        private readonly ICustomerProfileAppService _profiles;

        public CustomerProfileController(ICustomerProfileAppService profiles)
        {
            _profiles = profiles;
        }

        [HttpGet]
        public async Task<ActionResult<CustomerProfileDto>> GetMine(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profiles.GetMyAsync());
        }

        [HttpPut]
        public async Task<ActionResult<CustomerProfileDto>> Update(
            [FromBody] UpdateCustomerProfileDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _profiles.UpdateAsync(dto));
        }
    }
}
