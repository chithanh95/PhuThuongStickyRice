using PhuThuongStickyRice.Application.Subscriptions;
using PhuThuongStickyRice.Application.Subscriptions.DTOs;
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
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionAppService _subscriptionAppService;

        public SubscriptionsController(ISubscriptionAppService subscriptionAppService)
        {
            _subscriptionAppService = subscriptionAppService;
        }

        /// <summary>Public: customer creates a daily subscription (/dat-dinh-ky).</summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Create(
            [FromBody] CreateSubscriptionDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _subscriptionAppService.CreateAsync(dto));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> GetList(
            [FromQuery] bool? activeOnly,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _subscriptionAppService.GetListAsync(activeOnly));
        }

        public sealed class SetActiveRequestModel
        {
            public bool Active { get; set; }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/active")]
        public async Task<ActionResult> SetActive(
            Guid id,
            [FromBody] SetActiveRequestModel model,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _subscriptionAppService.SetActiveAsync(id, model?.Active ?? false));
        }
    }
}
