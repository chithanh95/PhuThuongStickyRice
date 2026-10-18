

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.AuditLogs;
using PhuThuongStickyRice.Application.AuditLogs;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>System log viewer (item 104): admin, level filter + search.</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Authorize(Roles = "Admin")]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class AppLogsController : ControllerBase
        {
            private readonly IAppLogAppService _logs;

            public AppLogsController(IAppLogAppService logs)
            {
                _logs = logs;
            }

            [HttpGet]
            public async Task<ActionResult<PagedResultDto<AppLogDto>>> GetList(
                [FromQuery] string level, [FromQuery] string searchTerm,
                [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _logs.GetListAsync(new GetAppLogsInput
                {
                    Level = level,
                    SearchTerm = searchTerm,
                    SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 50 : pageSize),
                    MaxResultCount = pageSize < 1 ? 50 : pageSize
                }));
            }
        }
}
