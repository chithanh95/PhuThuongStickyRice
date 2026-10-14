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
    // Item 107: versioned prefix — both routes stay live.
    [Route("api/v1/[controller]")]
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
                FulfillmentMethod = model.FulfillmentMethod,
                TaxCode = model.TaxCode,
                CompanyName = model.CompanyName,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                IsGift = model.IsGift,
                GiftMessage = model.GiftMessage,
                RedeemPoints = model.RedeemPoints,
                ReferralCode = model.ReferralCode,
                DateOfBirth = model.DateOfBirth,
                IdempotencyKey = model.IdempotencyKey,
                RecaptchaToken = model.RecaptchaToken,
                Lines = model.Lines
                    .Select(x => new CreateOrderLineDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        SizeOptionId = x.SizeOptionId,
                        ToppingIds = x.ToppingIds ?? new System.Collections.Generic.List<Guid>(),
                        IsCombo = x.IsCombo,
                        Note = x.Note
                    })
                    .ToList()
            });

            return Ok(order);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null, [FromQuery] string status = null,
            [FromQuery] Guid? shipperId = null,
            CancellationToken cancellationToken = default)
        {
            var paged = await _orderAppService.GetListAsync(new GetOrdersInput
            {
                MaxResultCount = pageSize < 1 ? 20 : pageSize,
                SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 20 : pageSize),
                SearchTerm = searchTerm,
                Status = status,
                ShipperId = shipperId
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
        /// Public: last order info for a phone number (name + address only),
        /// used to auto-fill the checkout form. Returns 204 when no previous
        /// order with an address is found.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("last-by-phone")]
        public async Task<ActionResult> GetLastByPhone(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            var info = await _orderAppService.GetLastOrderInfoByPhoneAsync(phone);
            if (info == null)
            {
                return NoContent();
            }

            return Ok(info);
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

        /// <summary>
        /// Customer self-service: cancel a Pending order. The order number +
        /// phone in the body must match the order.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("track/cancel")]
        public async Task<ActionResult> CancelByCustomer(
            [FromBody] CancelOrderRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.CancelByCustomerAsync(new CancelOrderByCustomerInput
                {
                    OrderNumber = model?.OrderNumber,
                    CustomerPhone = model?.CustomerPhone
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Customer self-service: edit quantities of a Pending order.
        /// Totals are recalculated server-side.
        /// </summary>
        [AllowAnonymous]
        [HttpPut("track/lines")]
        public async Task<ActionResult> UpdateLinesByCustomer(
            [FromBody] UpdateOrderLinesRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.UpdateLinesByCustomerAsync(new UpdateOrderLinesInput
                {
                    OrderNumber = model?.OrderNumber,
                    CustomerPhone = model?.CustomerPhone,
                    Lines = model?.Lines
                        .Select(x => new UpdateOrderLineQtyDto
                        {
                            LineId = x.LineId,
                            Quantity = x.Quantity
                        })
                        .ToList() ?? new System.Collections.Generic.List<UpdateOrderLineQtyDto>()
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Public: submit a star rating for a completed order.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("track/review")]
        public async Task<ActionResult> SubmitReview(
            [FromBody] SubmitReviewRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var review = await _orderAppService.SubmitReviewAsync(new SubmitReviewInput
                {
                    OrderNumber = model?.OrderNumber,
                    CustomerPhone = model?.CustomerPhone,
                    Rating = model?.Rating ?? 0,
                    Comment = model?.Comment
                });
                return Ok(review);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Admin: order reviews, newest first.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("reviews")]
        public async Task<ActionResult> GetReviews(CancellationToken cancellationToken = default)
        {
            return Ok(await _orderAppService.GetReviewsAsync());
        }

        /// <summary>
        /// Public: post-checkout lucky spin. One spin per order; a winning
        /// segment generates a real single-use voucher tied to the phone.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("spin")]
        public async Task<ActionResult> Spin(
            [FromBody] SpinRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _orderAppService.SpinAsync(new SpinInput
                {
                    OrderId = model?.OrderId ?? Guid.Empty,
                    CustomerPhone = model?.CustomerPhone
                });
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Item 82: admin assigns (or unassigns) a delivery shipper.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/assign-shipper")]
        public async Task<ActionResult> AssignShipper(Guid id,
            [FromBody] AssignShipperRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.AssignShipperAsync(id, new AssignShipperDto
                {
                    ShipperId = model?.ShipperId
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Item 81: kitchen display — Pending/Confirmed/Preparing orders.</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("kitchen")]
        public async Task<ActionResult> GetKitchenOrders(CancellationToken cancellationToken = default)
        {
            return Ok(await _orderAppService.GetKitchenOrdersAsync());
        }

        /// <summary>Item 81: kitchen display tap advances one stage.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:guid}/kitchen/advance")]
        public async Task<ActionResult> AdvanceKitchenStatus(Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _orderAppService.AdvanceKitchenStatusAsync(id));
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Item 87: admin hides/unhides a review.</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("reviews/{id:guid}/hide")]
        public async Task<ActionResult> SetReviewHidden(Guid id,
            [FromBody] SetReviewHiddenRequestModel model,
            CancellationToken cancellationToken = default)
        {
            var review = await _orderAppService.SetReviewHiddenAsync(id, new SetReviewHiddenDto
            {
                IsHidden = model?.IsHidden ?? false
            });
            return Ok(review);
        }

        /// <summary>Item 87: admin replies to a review (shown publicly).</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("reviews/{id:guid}/reply")]
        public async Task<ActionResult> ReplyReview(Guid id,
            [FromBody] ReplyReviewRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var review = await _orderAppService.ReplyReviewAsync(id, new ReplyReviewDto
                {
                    Reply = model?.Reply
                });
                return Ok(review);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Item 87: public non-hidden reviews for the landing page.</summary>
        [AllowAnonymous]
        [HttpGet("reviews/public")]
        public async Task<ActionResult> GetPublicReviews(CancellationToken cancellationToken = default)
        {
            return Ok(await _orderAppService.GetPublicReviewsAsync());
        }
    }

    public sealed class AssignShipperRequestModel
    {
        /// <summary>Shipper id; null unassigns.</summary>
        public Guid? ShipperId { get; set; }
    }

    public sealed class SetReviewHiddenRequestModel
    {
        public bool IsHidden { get; set; }
    }

    public sealed class ReplyReviewRequestModel
    {
        public string Reply { get; set; }
    }
}
