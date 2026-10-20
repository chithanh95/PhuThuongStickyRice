using PhuThuongStickyRice.Application.Catalog;
using PhuThuongStickyRice.Application.Catalog.DTOs;
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
    /// API báo giá tiệc/số lượng lớn: khách gửi yêu cầu báo giá công khai
    /// (không thanh toán ở bước này), admin quản lý (duyệt trạng thái, đặt
    /// cọc), kèm đăng ký nhắc xôi cúng Rằm/mùng 1.
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteRequestAppService _appService;

        public QuotesController(IQuoteRequestAppService appService)
        {
            _appService = appService;
        }

        /// <summary>
        /// Public: customer submits a bulk/event quote request (đặt tiệc).
        /// No payment is taken for quotes.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateQuoteRequestDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: danh sách yêu cầu báo giá phân trang (lọc theo từ khóa và
        /// trạng thái). page/pageSize không hợp lệ được kẹp về 1/20 để không
        /// gây SkipCount âm hay truy vấn quá lớn.
        /// </summary>
        /// <param name="page">Trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số dòng/trang.</param>
        /// <param name="searchTerm">Tìm theo tên/SĐT khách.</param>
        /// <param name="status">Lọc trạng thái xử lý.</param>
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null,
            [FromQuery] string status = null,
            CancellationToken cancellationToken = default)
        {
            var paged = await _appService.GetListAsync(new GetQuoteRequestsInput
            {
                MaxResultCount = pageSize < 1 ? 20 : pageSize,
                SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 20 : pageSize),
                SearchTerm = searchTerm,
                Status = status
            });

            return Ok(paged);
        }

        /// <summary>
        /// Public: subscribes to monthly xoi-cung reminders (Rằm / mùng 1).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("cung-reminders")]
        public async Task<ActionResult> CreateCungReminder(
            [FromBody] CreateCungReminderDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.CreateReminderAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Admin: lists cung reminder subscriptions.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("cung-reminders")]
        public async Task<ActionResult> GetCungReminders(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _appService.GetRemindersAsync());
        }

        /// <summary>Admin: toggles a cung reminder subscription on/off.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("cung-reminders/{id:guid}/toggle")]
        public async Task<ActionResult> ToggleCungReminder(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.ToggleReminderAsync(id));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Admin: cập nhật trạng thái yêu cầu báo giá (ví dụ: duyệt/từ chối).
        /// Lỗi nghiệp vụ (trạng thái không hợp lệ, không tìm thấy) trả 400.
        /// </summary>
        /// <param name="id">Id yêu cầu báo giá.</param>
        /// <param name="dto">Trạng thái mới.</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateQuoteRequestStatusDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.UpdateStatusAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Item 161: admin đặt cọc khi duyệt báo giá.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/deposit")]
        public async Task<ActionResult> SetDeposit(
            Guid id,
            [FromBody] SetQuoteDepositDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _appService.SetDepositAsync(id, dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
