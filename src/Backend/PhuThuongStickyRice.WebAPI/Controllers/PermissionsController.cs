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
    /// <summary>
    /// API ma trận phân quyền (Admin): xem và cập nhật quyền của từng vai trò
    /// nhân viên. Mọi thay đổi được ghi log hoạt động staff để truy vết.
    /// </summary>
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

        /// <summary>Lấy ma trận quyền hiện tại của các vai trò nhân viên.</summary>
        /// <param name="cancellationToken">Token hủy request.</param>
        [HttpGet("matrix")]
        public async Task<ActionResult> GetMatrix(CancellationToken cancellationToken = default)
        {
            return Ok(await _permissions.GetMatrixAsync());
        }

        /// <summary>
        /// Cập nhật quyền của 1 vai trò. Yêu cầu có trường Role; sau khi lưu
        /// ghi log hoạt động "Đổi phân quyền" để admin truy vết ai đổi quyền.
        /// </summary>
        /// <param name="input">Vai trò và danh sách mã quyền được cấp.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
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
