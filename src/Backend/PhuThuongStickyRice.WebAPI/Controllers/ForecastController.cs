

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Inventory;
using PhuThuongStickyRice.Application.Inventory;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        [Authorize(Roles = "Admin")]
        public class ForecastController : ControllerBase
        {
            private readonly IIngredientForecastAppService _forecast;

            public ForecastController(IIngredientForecastAppService forecast)
            {
                _forecast = forecast;
            }

            [HttpGet]
            public async Task<ActionResult<IngredientForecastDto>> Get(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _forecast.GetForecastAsync());
            }
        }
}
