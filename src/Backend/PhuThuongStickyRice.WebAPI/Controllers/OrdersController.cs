// Bật nullable annotation context ở cấp file: project chưa bật <Nullable>enable</Nullable>
// toàn cục nên compiler sẽ bỏ qua các annotation string?/IFormFile? trong file
// (CS8632) nếu thiếu dòng này. Convention theo
// RateLimiterPolicies/DefaultRateLimiterPolicy.cs.
#nullable enable
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.WebAPI.RateLimiterPolicies;
using PhuThuongStickyRice.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedReviewPhotoExtensions =
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxReviewPhotoBytes = 5 * 1024 * 1024;

        public OrdersController(
            IOrderAppService orderAppService,
            ILogger<OrdersController> logger,
            IWebHostEnvironment environment)
        {
            _orderAppService = orderAppService;
            _logger = logger;
            _environment = environment;
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
                PickupShipperName = model.PickupShipperName,
                PickupShipperPhone = model.PickupShipperPhone,
                TaxCode = model.TaxCode,
                CompanyName = model.CompanyName,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                IsGift = model.IsGift,
                GiftMessage = model.GiftMessage,
                RedeemPoints = model.RedeemPoints,
                ReferralCode = model.ReferralCode,
                DateOfBirth = model.DateOfBirth,
                // Bug fix: BranchId khách chọn ở checkout bị nuốt im lặng —
                // CreateOrderDto có BranchId nhưng request model thiếu property
                // này nên đơn tạo ra luôn BranchId = null dù backend (item 109)
                // đã xử lý đầy đủ (validate chi nhánh, lưu tên lên đơn).
                BranchId = model.BranchId,
                IdempotencyKey = model.IdempotencyKey,
                RecaptchaToken = model.RecaptchaToken,
                Source = model.Source,
                // "lines": null trong body sẽ ghi đè initializer thành null —
                // không guard sẽ NRE 500 tại .Select; để service trả 400 chuẩn
                // (CreateAsync ném DomainException "Order must contain at least one line.").
                Lines = (model.Lines ?? new System.Collections.Generic.List<CreateOrderLineRequestModel>())
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

        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string searchTerm = null, [FromQuery] string status = null,
            [FromQuery] Guid? shipperId = null,
            // Wave 6c (item 35): date-only range filter (Vietnam day boundaries).
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            // Item 163: filter by cash-collection state.
            [FromQuery] bool? cashCollected = null,
            // Item 130 (đúng): true = only VIP customers' orders.
            [FromQuery] bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            var paged = await _orderAppService.GetListAsync(new GetOrdersInput
            {
                MaxResultCount = pageSize < 1 ? 20 : pageSize,
                SkipCount = (page < 1 ? 0 : page - 1) * (pageSize < 1 ? 20 : pageSize),
                SearchTerm = searchTerm,
                Status = status,
                ShipperId = shipperId,
                FromDate = fromDate,
                ToDate = toDate,
                CashCollected = cashCollected,
                VipOnly = vipOnly
            });

            return Ok(paged);
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("customers")]
        public async Task<ActionResult> GetCustomers(
            [FromQuery] string searchTerm = null,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
        {
            var customers = await _orderAppService.GetCustomersAsync(searchTerm, skip, take);
            return Ok(customers);
        }

        /// <summary>
        /// Item 131: khách quá N ngày không có đơn (để winback).
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("customers/inactive")]
        public async Task<ActionResult> GetInactiveCustomers(
            [FromQuery] int days = 21,
            [FromQuery] int take = 50,
            CancellationToken cancellationToken = default)
        {
            var customers = await _orderAppService.GetInactiveCustomersAsync(days, take);
            return Ok(customers);
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await _orderAppService.GetAsync(id);
            return Ok(order);
        }

        [Authorize(Roles = "Admin,Cashier")]
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
        /// Item 143 (Public): first-order voucher for a new customer.
        /// Returns { isNewCustomer: false } for returning customers.
        /// 400 on an invalid phone.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("first-order-voucher")]
        public async Task<ActionResult> GetFirstOrderVoucher(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _orderAppService.GetFirstOrderVoucherAsync(phone));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Public: full order history for a VN phone number (no login).
        /// Strict VN mobile format is validated; 400 on invalid format.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("by-phone")]
        public async Task<ActionResult> GetByPhone(
            [FromQuery] string phone,
            CancellationToken cancellationToken = default)
        {
            var orders = await _orderAppService.GetOrdersByPhoneAsync(phone);
            return Ok(orders);
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
        [Authorize(Roles = "Admin,Cashier")]
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
        /// Item 142: admin/cashier explicitly merges duplicate orders —
        /// the given orders' lines are merged into the primary order and
        /// the merged orders are cancelled with a note. Never automatic.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("merge")]
        public async Task<ActionResult> MergeOrders([FromBody] MergeOrdersInput model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.MergeOrdersAsync(new MergeOrdersInput
                {
                    PrimaryOrderId = model?.PrimaryOrderId ?? Guid.Empty,
                    OrderIds = model?.OrderIds
                });
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
        /// Customer self-service: change address / delivery slot of a Pending
        /// order. The order number + phone in the body must match the order.
        /// </summary>
        [AllowAnonymous]
        [HttpPut("track/address")]
        public async Task<ActionResult> UpdateAddressByCustomer(
            [FromBody] UpdateOrderAddressRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.UpdateAddressByCustomerAsync(new UpdateOrderAddressByCustomerInput
                {
                    OrderNumber = model?.OrderNumber,
                    CustomerPhone = model?.CustomerPhone,
                    CustomerAddress = model?.CustomerAddress,
                    DeliverySlot = model?.DeliverySlot
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Public: customer self-service — update recipient phone + notes of a Pending order.
        /// Item 138.
        /// </summary>
        [AllowAnonymous]
        [HttpPut("track/contact")]
        public async Task<ActionResult> UpdateContactByCustomer(
            [FromBody] UpdateOrderContactRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.UpdateContactByCustomerAsync(new UpdateOrderContactByCustomerInput
                {
                    OrderNumber = model?.OrderNumber,
                    CustomerPhone = model?.CustomerPhone,
                    RecipientPhone = model?.RecipientPhone,
                    Notes = model?.Notes
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
                    Comment = model?.Comment,
                    PhotoUrl = model?.PhotoUrl,
                    Lines = model?.Lines?
                        .Select(x => new SubmitReviewLineInput
                        {
                            ProductId = x.ProductId,
                            Rating = x.Rating
                        })
                        .ToList() ?? new System.Collections.Generic.List<SubmitReviewLineInput>()
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
        /// Public: uploads a photo to attach to a review. The photo is saved
        /// immediately and its URL must be passed as PhotoUrl when submitting
        /// the review. Validates the order belongs to the phone.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("track/review/photo")]
        [RequestSizeLimit(MaxReviewPhotoBytes)]
        public async Task<ActionResult> UploadReviewPhoto(
            [FromForm] IFormFile file,
            [FromForm] string orderNumber,
            [FromForm] string customerPhone,
            CancellationToken cancellationToken = default)
        {
            var validationError = await ValidateReviewPhotoAsync(file, cancellationToken);
            if (validationError != null)
            {
                return BadRequest(new { message = validationError });
            }

            // The order must exist and belong to this phone.
            OrderDto order;
            try
            {
                order = await _orderAppService.TrackOrderAsync(new TrackOrderInput
                {
                    OrderNumber = orderNumber,
                    CustomerPhone = customerPhone
                });
            }
            catch (DomainException)
            {
                return BadRequest(new { message = "Không tìm thấy đơn hàng." });
            }

            if (order == null)
            {
                return BadRequest(new { message = "Không tìm thấy đơn hàng." });
            }

            var photoUrl = await SaveReviewPhotoAsync(file, cancellationToken);
            // Trả key "url" (không phải "photoUrl") cho khớp với 2 endpoint
            // upload ảnh còn lại (products, landing) và property Url phía
            // frontend (ShopEngagementService.ReviewPhotoUploadResult) —
            // bug-hunt run 16:05: key "photoUrl" không map được vào Url nên
            // frontend luôn nhận null, ảnh đánh giá không bao giờ đính kèm.
            return Ok(new { url = photoUrl });
        }

        /// <summary>
        /// Kiểm tra file ảnh đánh giá: bắt buộc có file, đúng định dạng
        /// (jpg/jpeg/png/webp/gif), chữ ký file (magic bytes) khớp đuôi ảnh
        /// và không vượt quá <see cref="MaxReviewPhotoBytes"/>. Trả về null
        /// khi hợp lệ, ngược lại là thông báo lỗi tiếng Việt.
        /// </summary>
        private static async Task<string?> ValidateReviewPhotoAsync(
            IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return "Vui lòng chọn một file ảnh.";
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension)
                || !AllowedReviewPhotoExtensions.Contains(extension.ToLowerInvariant()))
            {
                return "Định dạng ảnh không hợp lệ. Chỉ nhận: jpg, jpeg, png, webp, gif.";
            }

            // bug-hunt 2026-10-19: kiểm tra chữ ký file (magic bytes) — đuôi
            // ảnh có thể là file thực thi/script đổi tên; chỉ nhận nội dung
            // ảnh thật (JPEG/PNG/GIF/WebP).
            if (!await ImageUploadValidator.HasValidImageSignatureAsync(file, extension, cancellationToken))
            {
                return "File không phải ảnh hợp lệ (nội dung không khớp định dạng ảnh).";
            }

            if (file.Length > MaxReviewPhotoBytes)
            {
                return "File ảnh tối đa 5 MB.";
            }

            return null;
        }

        /// <summary>
        /// Lưu file ảnh đánh giá vào wwwroot/uploads/review-photos với tên
        /// ngẫu nhiên (tránh trùng và lộ tên gốc), trả về URL tương đối để
        /// client gửi kèm khi submit đánh giá.
        /// </summary>
        private async Task<string> SaveReviewPhotoAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var folder = Path.Combine((_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot")), "uploads", "review-photos");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            var filePath = Path.Combine(folder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            return $"/uploads/review-photos/{fileName}";
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
        [Authorize(Roles = "Admin,Cashier")]
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

        /// <summary>
        /// Wave 6c (item 33): admin sets or clears the internal order note.
        /// The note never appears on public endpoints.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPut("{id:guid}/admin-note")]
        public async Task<ActionResult> UpdateAdminNote(Guid id,
            [FromBody] UpdateAdminNoteRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.UpdateAdminNoteAsync(id, new UpdateAdminNoteDto
                {
                    Note = model?.Note
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Wave 6c (item 34): status-transition timeline of an order,
        /// oldest first. Kitchen staff see it on the /bep cards.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier,Kitchen")]
        [HttpGet("{id:guid}/status-history")]
        public async Task<ActionResult> GetStatusHistory(Guid id,
            CancellationToken cancellationToken = default)
        {
            return Ok(await _orderAppService.GetStatusHistoryAsync(id));
        }

        /// <summary>
        /// Staff confirms a ShipperPickup order was collected at the shop
        /// ("Xác nhận đã lấy hàng"). Optionally records the customer-booked
        /// shipper info. A Preparing order advances to Delivering.
        /// </summary>
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPut("{id:guid}/mark-picked-up")]
        public async Task<ActionResult> MarkPickedUp(Guid id,
            [FromBody] MarkPickedUpRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _orderAppService.MarkPickedUpAsync(id, new MarkPickedUpDto
                {
                    PickupShipperName = model?.PickupShipperName,
                    PickupShipperPhone = model?.PickupShipperPhone
                });
                return Ok(order);
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Item 81: kitchen display — Pending/Confirmed/Preparing orders.</summary>
        [Authorize(Roles = "Admin,Kitchen")]
        [HttpGet("kitchen")]
        public async Task<ActionResult> GetKitchenOrders(CancellationToken cancellationToken = default)
        {
            return Ok(await _orderAppService.GetKitchenOrdersAsync());
        }

        /// <summary>Item 81: kitchen display tap advances one stage.</summary>
        [Authorize(Roles = "Admin,Kitchen")]
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

        /// <summary>Item 52: customer rates the shipper (1-5) for an order review.
        /// Requires the order phone to prove ownership of the review.</summary>
        [AllowAnonymous]
        [HttpPost("reviews/{id:guid}/shipper-rating")]
        public async Task<ActionResult> SetShipperRating(Guid id,
            [FromBody] SetShipperRatingRequestModel model,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var review = await _orderAppService.SetShipperRatingAsync(id, new SetShipperRatingDto
                {
                    Rating = model?.Rating ?? 0,
                    CustomerPhone = model?.CustomerPhone
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

    public sealed class SetShipperRatingRequestModel
    {
        public int Rating { get; set; }

        /// <summary>SĐT đặt hàng — xác thực quyền đánh giá review này.</summary>
        public string CustomerPhone { get; set; }
    }
}
