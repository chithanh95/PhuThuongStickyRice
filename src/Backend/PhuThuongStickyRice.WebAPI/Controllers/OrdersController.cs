using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
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
                CustomerEmail = model.CustomerEmail,
                CustomerAddress = model.CustomerAddress,
                Notes = model.Notes,
                PaymentMethod = model.PaymentMethod,
                DiscountCode = model.DiscountCode,
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
        [HttpGet("customers")]
        public async Task<ActionResult> GetCustomers(
            [FromQuery] string searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var customers = await _orderAppService.GetCustomersAsync(searchTerm);
            return Ok(customers);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _orderAppService.GetAsync(id);
            return Ok(order);
        }

        [Authorize(Roles = "Admin")]
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

        /// <summary>
        /// Public order lookup: order number + phone (used by /track-order).
        /// Not-found is mapped to 404 by the GlobalExceptionFilter.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("track")]
        public async Task<ActionResult> Track(
            [FromQuery] string orderNumber, [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            var order = await _orderAppService.TrackOrderAsync(new TrackOrderInput
            {
                OrderNumber = orderNumber,
                CustomerPhone = phone
            });
            return Ok(order);
        }

        /// <summary>
        /// Customer reports they sent the bank transfer. Public, but the
        /// phone in the body must match the order's phone.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/mark-transferred")]
        public async Task<ActionResult> MarkTransferred(Guid id,
            [FromBody] MarkTransferredRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.MarkTransferredAsync(id, new MarkTransferredDto
                {
                    CustomerPhone = model?.CustomerPhone
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Admin confirms a bank-transfer payment.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/confirm-payment")]
        public async Task<ActionResult> ConfirmPayment(Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.ConfirmPaymentAsync(id);
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Creates a MoMo payment for a MoMo order. Public. In TestMode
        /// returns a payUrl to the in-app simulator; otherwise the real MoMo payUrl.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/momo/create")]
        public async Task<ActionResult> CreateMoMoPayment(Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _orderAppService.CreateMoMoPaymentAsync(id);
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
