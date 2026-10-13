using MediatR;
using PhuThuongStickyRice.Application.Orders.Commands;
using PhuThuongStickyRice.Application.Orders.Queries;
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
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        {
            _mediator = mediator;
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

            var order = await _mediator.Send(new CreateOrderCommand
            {
                CustomerName = model.CustomerName,
                CustomerPhone = model.CustomerPhone,
                CustomerAddress = model.CustomerAddress,
                Notes = model.Notes,
                Lines = model.Lines
                    .Select(x => new CreateOrderLineRequest
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity
                    })
                    .ToList()
            }, cancellationToken);

            return Ok(order);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null, [FromQuery] string status = null,
            CancellationToken cancellationToken = default)
        {
            var paged = await _mediator.Send(new GetOrdersQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                Status = status
            }, cancellationToken);

            return Ok(paged);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _mediator.Send(new GetOrderByIdQuery { OrderId = id }, cancellationToken);
            return Ok(order);
        }

        [Authorize]
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequestModel model,
            CancellationToken cancellationToken = default)
        {
            var order = await _mediator.Send(new UpdateOrderStatusCommand
            {
                OrderId = id,
                Status = model.Status
            }, cancellationToken);

            return Ok(order);
        }
    }
}
