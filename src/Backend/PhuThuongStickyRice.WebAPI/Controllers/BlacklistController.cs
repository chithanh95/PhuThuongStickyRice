

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Security;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 88: phone blacklist (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class BlacklistController : ControllerBase
    {
        private readonly IBlacklistAppService _blacklist;

        public BlacklistController(IBlacklistAppService blacklist)
        {
            _blacklist = blacklist;
        }

        /// <summary>Liệt kê danh sách SĐT bị chặn (Admin).</summary>
        [HttpGet]
        public async Task<ActionResult> GetList(CancellationToken cancellationToken = default)
        {
            return Ok(await _blacklist.GetListAsync());
        }

        /// <summary>Thêm SĐT vào danh sách chặn (Admin); lỗi nghiệp vụ → 400.</summary>
        /// <param name="dto">Thông tin SĐT bị chặn (số điện thoại + lý do).</param>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateBlacklistedPhoneDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _blacklist.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Gỡ SĐT khỏi danh sách chặn theo id (Admin), trả 204.</summary>
        /// <param name="id">Id của bản ghi chặn cần xóa.</param>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _blacklist.DeleteAsync(id);
            return NoContent();
        }
    }
}
