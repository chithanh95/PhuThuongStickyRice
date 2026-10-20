

using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhuThuongStickyRice.Application.Customer;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Item 89: CRM notes per customer phone (Admin).</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerNotesController : ControllerBase
    {
        private readonly ICustomerNoteAppService _notes;

        public CustomerNotesController(ICustomerNoteAppService notes)
        {
            _notes = notes;
        }

        /// <summary>Lấy danh sách ghi chú CRM của khách theo SĐT (Admin).</summary>
        /// <param name="phone">Số điện thoại khách hàng.</param>
        [HttpGet]
        public async Task<ActionResult> GetByPhone(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _notes.GetByPhoneAsync(phone));
        }

        /// <summary>Thêm ghi chú CRM cho khách (Admin); lỗi nghiệp vụ → 400.</summary>
        /// <param name="dto">Nội dung ghi chú (SĐT khách + nội dung ghi chú).</param>
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateCustomerNoteDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _notes.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Xóa ghi chú CRM theo id (Admin), trả 204.</summary>
        /// <param name="id">Id của ghi chú cần xóa.</param>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            await _notes.DeleteAsync(id);
            return NoContent();
        }
    }
}
