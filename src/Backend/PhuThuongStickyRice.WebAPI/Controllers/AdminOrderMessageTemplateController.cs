using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>
    /// Item 162: message templates per order status (mẫu tin nhắn).
    /// Admin edits the templates; Admin/Cashier fetch a rendered preview
    /// for an order and copy it manually (nothing is auto-sent).
    /// </summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/admin/order-message-templates")]
    [ApiController]
    public class AdminOrderMessageTemplateController : ControllerBase
    {
        private readonly IOrderMessageTemplateAppService _templates;

        public AdminOrderMessageTemplateController(IOrderMessageTemplateAppService templates)
        {
            _templates = templates;
        }

        /// <summary>Admin: lấy toàn bộ mẫu tin nhắn theo trạng thái đơn (để sửa ở màn hình quản trị).</summary>
        [HttpGet]
        public async Task<ActionResult<List<OrderMessageTemplateDto>>> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _templates.GetListAsync());
        }

        /// <summary>Admin: cập nhật nội dung mẫu tin nhắn của 1 trạng thái đơn (thay đổi áp dụng ngay cho lần xem trước tiếp theo).</summary>
        /// <param name="id">Mã mẫu tin nhắn cần sửa.</param>
        /// <param name="dto">Nội dung mẫu mới.</param>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<OrderMessageTemplateDto>> Update(
            Guid id,
            [FromBody] CreateUpdateOrderMessageTemplateDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _templates.UpdateAsync(id, dto));
            }
            catch (Domain.Exceptions.DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Renders the active template for a status with an order's data.
        /// Route matches the spec: GET api/admin/orders/{id}/message-preview?status=.
        /// Cashiers may also use it (copy button on the order list).
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("/api/admin/orders/{id:guid}/message-preview")]
        public async Task<ActionResult<OrderMessagePreviewDto>> GetPreview(
            Guid id,
            [FromQuery] string status,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _templates.GetPreviewAsync(id, status));
        }
    }
}
