

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Group orders (item 117): public create / view / add / close.</summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class GroupOrdersController : ControllerBase
        {
            private readonly IGroupOrderAppService _groups;

            public GroupOrdersController(IGroupOrderAppService groups)
            {
                _groups = groups;
            }

            [AllowAnonymous]
            [HttpPost]
            public async Task<ActionResult<GroupOrderDto>> Create(
                [FromBody] CreateGroupOrderDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _groups.CreateAsync(dto));
            }

            [AllowAnonymous]
            [HttpGet("{code}")]
            public async Task<ActionResult<GroupOrderDto>> Get(string code,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _groups.GetByCodeAsync(code));
            }

            [AllowAnonymous]
            [HttpPost("{code}/lines")]
            public async Task<ActionResult<GroupOrderDto>> AddLine(
                string code, [FromBody] AddGroupOrderLineDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _groups.AddLineAsync(code, dto));
            }

            [AllowAnonymous]
            [HttpPost("{code}/close")]
            public async Task<ActionResult<CloseGroupOrderResultDto>> Close(
                string code, [FromBody] CloseGroupOrderDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _groups.CloseAsync(code, dto));
            }
        }
}
