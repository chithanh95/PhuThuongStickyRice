

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
using PhuThuongStickyRice.Application.Common;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
        /// <summary>Webhooks (item 110): admin CRUD + test + delivery log.</summary>
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

            [HttpGet]
            public async Task<ActionResult<List<WebhookEndpointDto>>> GetList(
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.GetListAsync());
            }

            [HttpGet("{id}")]
            public async Task<ActionResult<WebhookEndpointDto>> Get(Guid id,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.GetAsync(id));
            }

            [HttpPost]
            public async Task<ActionResult<WebhookEndpointDto>> Create(
                [FromBody] CreateUpdateWebhookEndpointDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.CreateAsync(dto));
            }

            [HttpPut("{id}")]
            public async Task<ActionResult<WebhookEndpointDto>> Update(
                Guid id, [FromBody] CreateUpdateWebhookEndpointDto dto,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.UpdateAsync(id, dto));
            }

            [HttpDelete("{id}")]
            public async Task<ActionResult> Delete(Guid id,
                CancellationToken cancellationToken = default)
            {
                await _webhooks.DeleteAsync(id);
                return Ok();
            }

            /// <summary>Admin "Gửi thử" button: posts a Ping event to the endpoint.</summary>
            [HttpPost("{id}/test")]
            public async Task<ActionResult<WebhookDeliveryDto>> SendTest(Guid id,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.SendTestAsync(id));
            }

            [HttpGet("deliveries")]
            public async Task<ActionResult<List<WebhookDeliveryDto>>> GetDeliveries(
                [FromQuery] Guid? endpointId, [FromQuery] int maxCount = 50,
                CancellationToken cancellationToken = default)
            {
                return Ok(await _webhooks.GetDeliveriesAsync(endpointId, maxCount));
            }
        }
}
