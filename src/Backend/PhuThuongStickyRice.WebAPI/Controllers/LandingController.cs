using PhuThuongStickyRice.Application.Landing;
using PhuThuongStickyRice.Application.Landing.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LandingController : ControllerBase
    {
        private readonly ILandingAppService _landingAppService;

        public LandingController(ILandingAppService landingAppService)
        {
            _landingAppService = landingAppService;
        }

        [AllowAnonymous]
        [HttpGet("content")]
        public async Task<ActionResult> GetContent(CancellationToken cancellationToken = default)
        {
            var content = await _landingAppService.GetContentAsync();
            return Ok(content);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("content")]
        public async Task<ActionResult> UpdateContent(
            [FromBody] LandingContentDto dto,
            CancellationToken cancellationToken = default)
        {
            var content = await _landingAppService.UpdateContentAsync(dto);
            return Ok(content);
        }
    }
}
