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
    /// API phiếu hỗ trợ (/ho-tro): khách gửi yêu cầu hỗ trợ public (không cần
    /// đăng nhập), admin xem danh sách theo trạng thái và trả lời từng phiếu.
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
        /// Public: khách mở phiếu hỗ trợ ở trang /ho-tro (không cần đăng nhập).
        /// Lỗi nghiệp vụ (ví dụ SĐT sai định dạng) trả 400 kèm thông điệp.
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
        /// Admin xem danh sách phiếu hỗ trợ, lọc theo trạng thái
        /// (<paramref name="status"/> null = lấy tất cả).
        /// </summary>
        /// <param name="status">Trạng thái cần lọc; null để lấy tất cả.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] string status,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _ticketAppService.GetListAsync(status));
        }

        /// <summary>
        /// Admin trả lời một phiếu hỗ trợ. Trả lời xong phiếu chuyển sang
        /// trạng thái đã xử lý (do service quyết định).
        /// </summary>
        /// <param name="id">Id phiếu hỗ trợ.</param>
        /// <param name="dto">Nội dung trả lời.</param>
        /// <param name="cancellationToken">Token hủy request.</param>
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
