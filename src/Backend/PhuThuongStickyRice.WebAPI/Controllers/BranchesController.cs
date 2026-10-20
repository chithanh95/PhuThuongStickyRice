

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Shop;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>
        /// Item 109: chi nhánh quán. Hai route song song: <c>api/Branches</c>
        /// và <c>api/v1/Branches</c> (tương thích shop cũ đang gọi route v1).
        /// Endpoint public chỉ trả chi nhánh đang hoạt động cho dropdown chọn
        /// chi nhánh của shop; CRUD còn lại yêu cầu quyền Admin.
        /// </summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [Route("api/v1/[controller]")]
        [ApiController]
        public class BranchesController : ControllerBase
        {
            private readonly IBranchAppService _branches;

            public BranchesController(IBranchAppService branches)
            {
                _branches = branches;
            }

            /// <summary>Public: các chi nhánh đang hoạt động cho shop chọn.</summary>
            [AllowAnonymous]
            [HttpGet]
            public async Task<ActionResult<List<BranchDto>>> GetActive(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.GetActiveAsync());
            }

            /// <summary>Admin: toàn bộ chi nhánh (kể cả đã tắt) cho màn hình quản lý.</summary>
            [Authorize(Roles = "Admin")]
            [HttpGet("admin")]
            public async Task<ActionResult<List<BranchDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.GetListAsync());
            }

            /// <summary>Admin: chi tiết 1 chi nhánh (form sửa).</summary>
            [Authorize(Roles = "Admin")]
            [HttpGet("admin/{id}")]
            public async Task<ActionResult<BranchDto>> Get(Guid id,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.GetAsync(id));
            }

            /// <summary>Admin: thêm chi nhánh mới (tên, địa chỉ, giờ mở cửa).</summary>
            [Authorize(Roles = "Admin")]
            [HttpPost("admin")]
            public async Task<ActionResult<BranchDto>> Create(
                [FromBody] CreateUpdateBranchDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.CreateAsync(dto));
            }

            /// <summary>Admin: cập nhật chi nhánh.</summary>
            [Authorize(Roles = "Admin")]
            [HttpPut("admin/{id}")]
            public async Task<ActionResult<BranchDto>> Update(
                Guid id, [FromBody] CreateUpdateBranchDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _branches.UpdateAsync(id, dto));
            }

            /// <summary>Admin: xóa chi nhánh.</summary>
            [Authorize(Roles = "Admin")]
            [HttpDelete("admin/{id}")]
            public async Task<ActionResult> Delete(Guid id,
                CancellationToken cancellationToken = default)
            {
                await _branches.DeleteAsync(id);
                return Ok();
            }
        }
}
