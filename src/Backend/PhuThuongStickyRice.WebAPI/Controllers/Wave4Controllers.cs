using PhuThuongStickyRice.Application.Wave4;
using PhuThuongStickyRice.Application.Wave4.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    /// <summary>Feature flags (item 105): public enabled-map for UI gating + admin toggles.</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FeatureFlagsController : ControllerBase
    {
        private readonly IFeatureFlagAppService _flags;

        public FeatureFlagsController(IFeatureFlagAppService flags)
        {
            _flags = flags;
        }

        /// <summary>Public: key → enabled map used by the storefront to gate UI.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<Dictionary<string, bool>>> GetEnabledMap(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _flags.GetEnabledMapAsync());
        }

        /// <summary>Admin: all flags with metadata.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<List<FeatureFlagDto>>> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _flags.GetListAsync());
        }

        /// <summary>Admin: flip a flag.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{key}")]
        public async Task<ActionResult<FeatureFlagDto>> SetEnabled(
            string key, [FromBody] SetFeatureFlagEnabledDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _flags.SetEnabledAsync(key, dto.Enabled));
        }
    }

    public class SetFeatureFlagEnabledDto
    {
        public bool Enabled { get; set; }
    }

    /// <summary>Branches (item 109): public active list + admin CRUD.</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchAppService _branches;

        public BranchesController(IBranchAppService branches)
        {
            _branches = branches;
        }

        /// <summary>Public: active branches for the shop branch picker.</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<BranchDto>>> GetActive(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _branches.GetActiveAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<List<BranchDto>>> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _branches.GetListAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{id}")]
        public async Task<ActionResult<BranchDto>> Get(Guid id,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _branches.GetAsync(id));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin")]
        public async Task<ActionResult<BranchDto>> Create(
            [FromBody] CreateUpdateBranchDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _branches.CreateAsync(dto));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id}")]
        public async Task<ActionResult<BranchDto>> Update(
            Guid id, [FromBody] CreateUpdateBranchDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _branches.UpdateAsync(id, dto));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id}")]
        public async Task<ActionResult> Delete(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _branches.DeleteAsync(id);
            return Ok();
        }
    }

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

    /// <summary>Short links (item 116): admin CRUD + public create (quick-view "🔗 Link ngắn").</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ShortLinksController : ControllerBase
    {
        private readonly IShortLinkAppService _links;

        public ShortLinksController(IShortLinkAppService links)
        {
            _links = links;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<List<ShortLinkDto>>> GetList(
            CancellationToken cancellationToken = default)
        {
            return Ok(await _links.GetListAsync());
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<ShortLinkDto>> Create(
            [FromBody] CreateShortLinkDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _links.CreateAsync(dto));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id,
            CancellationToken cancellationToken = default)
        {
            await _links.DeleteAsync(id);
            return Ok();
        }
    }

    /// <summary>Short-link redirect: GET /s/{code} → 302 to the target URL (item 116).</summary>
    [Produces("application/json")]
    [Route("s")]
    [ApiController]
    public class ShortLinkRedirectController : ControllerBase
    {
        private readonly IShortLinkAppService _links;

        public ShortLinkRedirectController(IShortLinkAppService links)
        {
            _links = links;
        }

        [AllowAnonymous]
        [HttpGet("{code}")]
        public async Task<ActionResult> RedirectToTarget(string code,
            CancellationToken cancellationToken = default)
        {
            var target = await _links.ResolveAsync(code);
            if (string.IsNullOrWhiteSpace(target))
            {
                return NotFound("Liên kết không tồn tại.");
            }

            return Redirect(target);
        }
    }

    /// <summary>Group orders (item 117): public create / view / add / close.</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class GroupOrdersController : ControllerBase
    {
        private readonly IGroupOrderAppService _groups;

        public GroupOrdersController(IGroupOrderAppService groups)
        {
            _groups = groups;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<GroupOrderDto>> Create(
            [FromBody] CreateGroupOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _groups.CreateAsync(dto));
        }

        [AllowAnonymous]
        [HttpGet("{code}")]
        public async Task<ActionResult<GroupOrderDto>> Get(string code,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _groups.GetByCodeAsync(code));
        }

        [AllowAnonymous]
        [HttpPost("{code}/lines")]
        public async Task<ActionResult<GroupOrderDto>> AddLine(
            string code, [FromBody] AddGroupOrderLineDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _groups.AddLineAsync(code, dto));
        }

        [AllowAnonymous]
        [HttpPost("{code}/close")]
        public async Task<ActionResult<CloseGroupOrderResultDto>> Close(
            string code, [FromBody] CloseGroupOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _groups.CloseAsync(code, dto));
        }
    }

    /// <summary>Address book (item 119): public, keyed by customer phone.</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AddressBooksController : ControllerBase
    {
        private readonly IAddressBookAppService _addresses;

        public AddressBooksController(IAddressBookAppService addresses)
        {
            _addresses = addresses;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<AddressBookDto>>> GetByPhone(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.GetByPhoneAsync(phone));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<AddressBookDto>> Save(
            [FromBody] SaveAddressDto dto,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _addresses.SaveAsync(dto));
        }

        [AllowAnonymous]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id, [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            await _addresses.DeleteAsync(id, phone);
            return Ok();
        }
    }

    /// <summary>System log viewer (item 104): admin, level filter + search.</summary>
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class AppLogsController : ControllerBase
    {
        private readonly IAppLogAppService _logs;

        public AppLogsController(IAppLogAppService logs)
        {
            _logs = logs;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultDto<AppLogDto>>> GetList(
            [FromQuery] string level, [FromQuery] string searchTerm,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _logs.GetListAsync(new GetAppLogsInput
            {
                Level = level,
                SearchTerm = searchTerm,
                SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 50 : pageSize),
                MaxResultCount = pageSize < 1 ? 50 : pageSize
            }));
        }
    }
}
