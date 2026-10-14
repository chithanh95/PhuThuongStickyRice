using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.WebAPI.Controllers
{
    [EnableRateLimiting(RateLimiterPolicyNames.DefaultPolicy)]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderAppService _orderAppService;
        private readonly ILogger _logger;

        public OrdersController(IOrderAppService orderAppService, ILogger<OrdersController> logger)
        {
            _orderAppService = orderAppService;
            _logger = logger;
        }

        /// <summary>
        /// Places a new order. Public endpoint so guests can check out.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateOrderRequestModel model,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating order for {CustomerPhone}", model?.CustomerPhone);

            var order = await _orderAppService.CreateAsync(new CreateOrderDto
            {
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                CustomerAddress = model.CustomerAddress,
                Notes = model.Notes,
                Lines = model.Lines
                    .Select(x => new CreateOrderLineDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity
                    })
                    .ToList()
            });

            return Ok(order);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null, [FromQuery] string status = null,
            CancellationToken cancellationToken = default)
        {
            var paged = await _orderAppService.GetListAsync(new GetOrdersInput
            {
                MaxResultCount = pageSize < 1 ? 20 : pageSize,
                SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 20 : pageSize),
                SearchTerm = searchTerm,
                Status = status
            });

            return Ok(paged);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _orderAppService.GetAsync(id);
            return Ok(order);
        }

        [Authorize]
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequestModel model,
            CancellationToken cancellationToken = default)
        {
            var order = await _orderAppService.UpdateStatusAsync(id, new UpdateOrderStatusDto
            {
                Status = model.Status
            });

            return Ok(order);
        }
    }
}
