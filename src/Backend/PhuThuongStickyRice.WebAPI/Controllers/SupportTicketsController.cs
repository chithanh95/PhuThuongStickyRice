using PhuThuongStickyRice.Application.Support;
using PhuThuongStickyRice.Application.Support.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// API phiếu hỗ trợ (/ho-tro): khách tạo phiếu công khai không cần đăng
    /// nhập; admin xem danh sách theo trạng thái và trả lời từng phiếu.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SupportTicketsController : ControllerBase
    {
        private readonly ISupportTicketAppService _ticketAppService;

        public SupportTicketsController(ISupportTicketAppService ticketAppService)
        {
            _ticketAppService = ticketAppService;
        }

        /// <summary>
        /// Public: khách mở phiếu hỗ trợ mới trên trang /ho-tro.
        /// Lỗi nghiệp vụ (dữ liệu không hợp lệ...) → 400 thay vì 500.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateSupportTicketDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _ticketAppService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Admin: liệt kê phiếu hỗ trợ, lọc theo trạng thái (mở/đang xử lý/đóng).
        /// </summary>
        /// <param name="status">Trạng thái cần lọc; trống = tất cả.</param>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] string status,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _ticketAppService.GetListAsync(status));
        }

        /// <summary>
        /// Admin: trả lời một phiếu hỗ trợ của khách.
        /// </summary>
        /// <param name="id">Id phiếu hỗ trợ cần trả lời.</param>
        /// <param name="dto">Nội dung trả lời.</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/reply")]
        public async Task<ActionResult> Reply(
            Guid id,
            [FromBody] ReplySupportTicketDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _ticketAppService.ReplyAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
