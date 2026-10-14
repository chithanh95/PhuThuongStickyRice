using PhuThuongStickyRice.Application.Wave3;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 99: login history (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LoginAuditsController : ControllerBase
    {
        private readonly ILoginAuditAppService _loginAudits;

        public LoginAuditsController(ILoginAuditAppService loginAudits)
        {
            _loginAudits = loginAudits;
        }

        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 || pageSize > 200 ? 50 : pageSize;
            return Ok(await _loginAudits.GetListAsync((page - 1) * pageSize, pageSize));
        }
    }
}
