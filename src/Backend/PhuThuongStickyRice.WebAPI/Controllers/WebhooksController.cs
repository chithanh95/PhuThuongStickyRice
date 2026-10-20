

using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>
        /// Quản trị webhook (item 110): admin CRUD endpoint, gửi thử sự kiện
        /// và xem nhật ký các lần đẩy. Chỉ tài khoản Admin được truy cập.
        /// </summary>
        [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
        [Authorize(Roles = "Admin")]
        [Produces("application/json")]
        [Route("api/[controller]")]
        [ApiController]
        public class WebhooksController : ControllerBase
        {
            private readonly IWebhookAppService _webhooks;

            public WebhooksController(IWebhookAppService webhooks)
            {
                _webhooks = webhooks;
            }

            /// <summary>Lấy danh sách endpoint webhook đã đăng ký.</summary>
            [HttpGet]
            public async Task<ActionResult<List<WebhookEndpointDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.GetListAsync());
            }

            /// <summary>Lấy chi tiết 1 endpoint webhook theo id.</summary>
            /// <param name="id">Id của endpoint.</param>
            [HttpGet("{id}")]
            public async Task<ActionResult<WebhookEndpointDto>> Get(Guid id,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.GetAsync(id));
            }

            /// <summary>Đăng ký endpoint webhook mới (URL, sự kiện cần nhận, secret ký).</summary>
            /// <param name="dto">Thông tin endpoint cần tạo.</param>
            [HttpPost]
            public async Task<ActionResult<WebhookEndpointDto>> Create(
                [FromBody] CreateUpdateWebhookEndpointDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.CreateAsync(dto));
            }

            /// <summary>Cập nhật thông tin endpoint webhook (URL, sự kiện, secret, trạng thái).</summary>
            /// <param name="id">Id của endpoint cần sửa.</param>
            /// <param name="dto">Thông tin mới của endpoint.</param>
            [HttpPut("{id}")]
            public async Task<ActionResult<WebhookEndpointDto>> Update(
                Guid id, [FromBody] CreateUpdateWebhookEndpointDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.UpdateAsync(id, dto));
            }

            /// <summary>Xóa endpoint webhook — hệ thống ngừng đẩy sự kiện tới URL này.</summary>
            /// <param name="id">Id của endpoint cần xóa.</param>
            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id,
                CancellationToken cancellationToken = default)
            {
                await _webhooks.DeleteAsync(id);
                return Ok();
            }

            /// <summary>
            /// Nút "Gửi thử" của admin: đẩy 1 sự kiện Ping tới endpoint để kiểm
            /// tra URL/secret có nhận được không trước khi dùng thật.
            /// </summary>
            /// <param name="id">Id của endpoint cần gửi thử.</param>
            [HttpPost("{id}/test")]
            public async Task<ActionResult<WebhookDeliveryDto>> SendTest(Guid id,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.SendTestAsync(id));
            }

            /// <summary>
            /// Lấy nhật ký các lần đẩy webhook (thành công/thất bại) để đối soát
            /// với đối tác khi sự kiện không tới nơi.
            /// </summary>
            /// <param name="endpointId">Lọc theo endpoint; bỏ trống để xem tất cả.</param>
            /// <param name="maxCount">Số bản ghi tối đa trả về (mặc định 50).</param>
            [HttpGet("deliveries")]
            public async Task<ActionResult<List<WebhookDeliveryDto>>> GetDeliveries(
                [FromQuery] Guid? endpointId, [FromQuery] int maxCount = 50,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.GetDeliveriesAsync(endpointId, maxCount));
            }
        }
}
