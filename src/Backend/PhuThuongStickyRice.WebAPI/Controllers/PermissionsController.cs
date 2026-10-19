using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 155: fine-grained role permission matrix (Admin only).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/admin/permissions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;

        public PermissionsController(
            IPermissionService permissions,
            IStaffActivityLogger activityLogger)
        {
            _permissions = permissions;
            _activityLogger = activityLogger;
        }

        [HttpGet("matrix")]
        public async Task<ActionResult> GetMatrix(CancellationToken cancellationToken = default)
        {
            return Ok(await _permissions.GetMatrixAsync());
        }

        [HttpPut("matrix")]
        public async Task<ActionResult> UpdateMatrix([FromBody] UpdateRolePermissionsDto input,
            CancellationToken cancellationToken = default)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Role))
            {
                return BadRequest("Role is required.");
            }

            try
            {
                await _permissions.UpdateRolePermissionsAsync(input);
                await _activityLogger.LogAsync(StaffActivityActions.PermissionChange,
                    $"Đổi phân quyền vai trò {input.Role}: {(input.GrantedKeys?.Count ?? 0)} quyền.");
                return Ok(new { message = "Đã lưu phân quyền." });
            }
            catch (Volo.Abp.BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
