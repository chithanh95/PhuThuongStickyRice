

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

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        [Authorize(Roles = "Admin")]
        /// <summary>
        /// API dự báo nguyên liệu (Item 124): ước tính lượng nếp (kg) cần chuẩn bị
        /// cho sáng hôm sau, tính từ trung bình phần bán cùng thứ trong 4 tuần gần nhất
        /// (đơn đã hủy bị loại). Chỉ Admin được xem — đây là ước tính, không phải cam kết.
        /// </summary>
        public class ForecastController : ControllerBase
        {
            private readonly IIngredientForecastAppService _forecast;

            public ForecastController(IIngredientForecastAppService forecast)
            {
                _forecast = forecast;
            }

            /// <summary>
            /// Lấy báo cáo dự báo nguyên liệu hiện tại (lượng nếp cần cho sáng mai).
            /// </summary>
            [HttpGet]
            public async Task<ActionResult<IngredientForecastDto>> Get(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _forecast.GetForecastAsync());
            }
        }
}
