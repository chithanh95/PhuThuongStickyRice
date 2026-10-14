using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Email;
using PhuThuongStickyRice.Application.Orders.Exceptions;
using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Zalo;
using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using Volo.Abp.ObjectMapping;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IOrderAppService : IApplicationService
    {
        Task<OrderDto> CreateAsync(CreateOrderDto input);

        Task<PagedResultDto<OrderDto>> GetListAsync(GetOrdersInput input);

        Task<OrderDto> GetAsync(Guid id);

        Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto input);

        /// <summary>Customer marks their bank-transfer order as transferred.</summary>
        Task<OrderDto> MarkTransferredAsync(Guid id, MarkTransferredDto input);

        /// <summary>Admin confirms a payment (bank transfer).</summary>
        Task<OrderDto> ConfirmPaymentAsync(Guid id);

        /// <summary>Creates a MoMo payment for a MoMo order (test simulator or real payUrl).</summary>
        Task<MoMoCreatePaymentResultDto> CreateMoMoPaymentAsync(Guid id);

        /// <summary>Public order lookup by order number + phone.</summary>
        Task<OrderDto> TrackOrderAsync(TrackOrderInput input);

        /// <summary>Admin: customers aggregated by phone number.</summary>
        Task<List<CustomerDto>> GetCustomersAsync(string searchTerm);

        /// <summary>
        /// Public: last order info for a phone number (name + address only),
        /// used to auto-fill the checkout form.
        /// </summary>
        Task<LastOrderInfoDto> GetLastOrderInfoByPhoneAsync(string phone);

        /// <summary>Customer self-service: cancel a Pending order (phone must match).</summary>
        Task<OrderDto> CancelByCustomerAsync(CancelOrderByCustomerInput input);

        /// <summary>Customer self-service: edit quantities of a Pending order (phone must match).</summary>
        Task<OrderDto> UpdateLinesByCustomerAsync(UpdateOrderLinesInput input);

        /// <summary>Public: submit a star rating for a completed order.</summary>
        Task<OrderReviewDto> SubmitReviewAsync(SubmitReviewInput input);

        /// <summary>Admin: order reviews, newest first.</summary>
        Task<List<OrderReviewDto>> GetReviewsAsync();

        /// <summary>Public: post-checkout lucky spin — one spin per order, generates a real voucher.</summary>
        Task<SpinResultDto> SpinAsync(SpinInput input);

        /// <summary>
        /// Internal: cancels Pending bank-transfer orders that stayed unpaid
        /// past the configured expiry. Called by the background worker.
        /// Returns the number of orders cancelled.
        /// </summary>
        Task<int> AutoCancelExpiredBankTransfersAsync();
    }

    public class OrderAppService : ApplicationService, IOrderAppService
    {
        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;
        private readonly IEmailMessageRepository _emailMessages;
        private readonly BankTransferOptions _bankTransfer;
        private readonly IMoMoPaymentService _momo;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IDiscountAppService _discounts;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> _sizeOptions;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> _toppings;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Combo, Guid> _combos;
        private readonly ILoyaltyAppService _loyalty;
        private readonly IZaloNotificationService _zalo;
        private readonly Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> _reviews;

        public OrderAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            IEmailMessageRepository emailMessages,
            BankTransferOptions bankTransfer,
            IMoMoPaymentService momo,
            IShopSettingAppService shopSettings,
            IDiscountAppService discounts,
            Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> sizeOptions,
            Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> toppings,
            Volo.Abp.Domain.Repositories.IRepository<Combo, Guid> combos,
            ILoyaltyAppService loyalty,
            IZaloNotificationService zalo,
            Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> reviews)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
            _emailMessages = emailMessages;
            _bankTransfer = bankTransfer;
            _momo = momo;
            _shopSettings = shopSettings;
            _discounts = discounts;
            _sizeOptions = sizeOptions;
            _toppings = toppings;
            _combos = combos;
            _loyalty = loyalty;
            _zalo = zalo;
            _reviews = reviews;
        }

        public async Task<OrderDto> CreateAsync(CreateOrderDto input)
        {
            if (input.Lines == null || input.Lines.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Order must contain at least one line.");
            }

            if ((!Enum.TryParse<PaymentMethod>(input.PaymentMethod, ignoreCase: true, out var paymentMethod)
                    || !Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
                && !string.IsNullOrWhiteSpace(input.PaymentMethod))
            {
                throw new Domain.Exceptions.DomainException($"Unknown payment method '{input.PaymentMethod}'.");
            }

            // Fulfillment: delivery (default) or self-pickup at the shop.
            var fulfillment = FulfillmentMethod.Delivery;
            if (!string.IsNullOrWhiteSpace(input.FulfillmentMethod)
                && Enum.TryParse<FulfillmentMethod>(input.FulfillmentMethod, ignoreCase: true, out var parsedFulfillment)
                && Enum.IsDefined(typeof(FulfillmentMethod), parsedFulfillment))
            {
                fulfillment = parsedFulfillment;
            }

            var productIds = input.Lines.Where(x => !x.IsCombo).Select(x => x.ProductId).Distinct().ToList();
            var productQueryable = await _products.GetQueryableAsync();
            var products = await _asyncExecuter.ToListAsync(
                productQueryable.Where(x => productIds.Contains(x.Id)));

            var comboIds = input.Lines.Where(x => x.IsCombo).Select(x => x.ProductId).Distinct().ToList();
            var comboQueryable = await _combos.GetQueryableAsync();
            var combos = await _asyncExecuter.ToListAsync(
                comboQueryable.Where(x => comboIds.Contains(x.Id)));

            var sizeOptionIds = input.Lines
                .Where(x => x.SizeOptionId.HasValue)
                .Select(x => x.SizeOptionId.Value)
                .Distinct()
                .ToList();
            var sizeQueryable = await _sizeOptions.GetQueryableAsync();
            var sizeOptions = sizeOptionIds.Count == 0
                ? new List<ProductSizeOption>()
                : await _asyncExecuter.ToListAsync(
                    sizeQueryable.Where(x => sizeOptionIds.Contains(x.Id)));

            var toppingIds = input.Lines
                .SelectMany(x => x.ToppingIds ?? Enumerable.Empty<Guid>())
                .Distinct()
                .ToList();
            var toppingQueryable = await _toppings.GetQueryableAsync();
            var toppings = toppingIds.Count == 0
                ? new List<Topping>()
                : await _asyncExecuter.ToListAsync(
                    toppingQueryable.Where(x => toppingIds.Contains(x.Id) && x.IsActive));

            var lines = new List<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity,
                string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo)>();
            foreach (var line in input.Lines)
            {
                if (line.IsCombo)
                {
                    var combo = combos.FirstOrDefault(x => x.Id == line.ProductId);
                    if (combo == null || !combo.IsActive)
                    {
                        throw new Domain.Exceptions.DomainException("Combo không tồn tại hoặc đã ngừng bán.");
                    }

                    lines.Add((
                        combo.Id,
                        combo.Name,
                        Money.Of(combo.Price, "VND"),
                        line.Quantity,
                        null,
                        null,
                        0,
                        true));
                    continue;
                }

                var product = products.FirstOrDefault(x => x.Id == line.ProductId);
                if (product == null)
                {
                    throw new ProductNotFoundException(line.ProductId);
                }

                if (product.Status != ProductStatus.Active)
                {
                    throw new Domain.Exceptions.DomainException($"Product '{product.Name}' is not available for ordering.");
                }

                // Size option: replaces the base price with the size price.
                string sizeName = null;
                var unitPrice = product.Price;
                if (line.SizeOptionId.HasValue)
                {
                    var sizeOption = sizeOptions.FirstOrDefault(x => x.Id == line.SizeOptionId.Value);
                    if (sizeOption == null || !sizeOption.IsActive || sizeOption.ProductId != product.Id)
                    {
                        throw new Domain.Exceptions.DomainException(
                            $"Size option for '{product.Name}' is not available.");
                    }

                    sizeName = sizeOption.Name;
                    unitPrice = Money.Of(sizeOption.Price, product.Price.Currency);
                }

                // Toppings: extra charge summed on top of the unit price.
                string toppingNames = null;
                decimal toppingsExtra = 0;
                var lineToppings = (line.ToppingIds ?? Enumerable.Empty<Guid>()).Distinct().ToList();
                if (lineToppings.Count > 0)
                {
                    var resolved = toppings.Where(x => lineToppings.Contains(x.Id)).ToList();
                    if (resolved.Count != lineToppings.Count)
                    {
                        throw new Domain.Exceptions.DomainException("Một topping đã chọn không còn khả dụng.");
                    }

                    toppingNames = string.Join(", ", resolved.Select(x => x.Name));
                    toppingsExtra = resolved.Sum(x => x.ExtraPrice);
                    unitPrice = Money.Of(unitPrice.Amount + toppingsExtra, unitPrice.Currency);
                }

                lines.Add((product.Id, product.Name, unitPrice, line.Quantity,
                    sizeName, toppingNames, toppingsExtra, false));
            }

            var orderNumber = await GenerateOrderNumberAsync();

            var subtotal = lines.Sum(x => x.Quantity * x.UnitPrice.Amount);
            var currency = lines.First().UnitPrice.Currency;

            // Minimum order amount (shop setting).
            var shopSetting = await _shopSettings.GetAsync();
            if (shopSetting.MinOrderAmount > 0 && subtotal < shopSetting.MinOrderAmount)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Đơn hàng tối thiểu {shopSetting.MinOrderAmount:N0} ₫. Vui lòng thêm món vào giỏ.");
            }

            // Discount code (validated + usage counted here; owner-tied
            // spin/birthday codes require the matching phone).
            decimal discountValue = 0;
            string appliedCode = null;
            if (!string.IsNullOrWhiteSpace(input.DiscountCode))
            {
                discountValue = await _discounts.ApplyAsync(
                    input.DiscountCode, subtotal, input.CustomerPhone);
                appliedCode = input.DiscountCode.Trim().ToUpperInvariant();
            }

            // Loyalty account (created on first order) + tier discount.
            var loyaltyAccount = await _loyalty.GetOrCreateAsync(input.CustomerPhone);
            var (_, tierPercent) = _loyalty.GetTier(loyaltyAccount.TotalSpent, shopSetting);
            decimal tierValue = 0;
            if (loyaltyAccount.TotalSpent > 0 && tierPercent > 0)
            {
                tierValue = Math.Round((subtotal - discountValue) * tierPercent / 100, 0);
            }

            // Loyalty points redemption.
            int redeemedPoints = 0;
            decimal loyaltyValue = 0;
            if (input.RedeemPoints > 0)
            {
                (redeemedPoints, loyaltyValue) = await _loyalty.RedeemAsync(
                    input.CustomerPhone, input.RedeemPoints, shopSetting);
            }

            // Referral code: must exist and must not be the buyer's own.
            string referralCodeUsed = null;
            if (!string.IsNullOrWhiteSpace(input.ReferralCode))
            {
                var referrer = await _loyalty.FindByReferralCodeAsync(input.ReferralCode);
                if (referrer == null)
                {
                    throw new Domain.Exceptions.DomainException("Mã giới thiệu không tồn tại.");
                }

                if (referrer.Id == loyaltyAccount.Id)
                {
                    throw new Domain.Exceptions.DomainException(
                        "Không thể dùng mã giới thiệu của chính mình.");
                }

                referralCodeUsed = referrer.ReferralCode;
            }

            // Birthday saved to the loyalty profile when provided.
            if (input.DateOfBirth.HasValue)
            {
                loyaltyAccount.DateOfBirth = input.DateOfBirth.Value.Date;
                loyaltyAccount.UpdatedAt = DateTime.UtcNow;
            }

            // VAT invoice fields.
            var taxCode = string.IsNullOrWhiteSpace(input.TaxCode) ? null : input.TaxCode.Trim();
            var companyName = string.IsNullOrWhiteSpace(input.CompanyName) ? null : input.CompanyName.Trim();
            if (taxCode != null && companyName == null)
            {
                throw new Domain.Exceptions.DomainException(
                    "Vui lòng nhập tên công ty khi cần hóa đơn VAT.");
            }

            // Recipient (ordering for someone else).
            var recipientName = string.IsNullOrWhiteSpace(input.RecipientName) ? null : input.RecipientName.Trim();
            var recipientPhone = string.IsNullOrWhiteSpace(input.RecipientPhone) ? null : input.RecipientPhone.Trim();
            if ((recipientName == null) != (recipientPhone == null))
            {
                throw new Domain.Exceptions.DomainException(
                    "Vui lòng nhập đầy đủ tên và số điện thoại người nhận.");
            }

            // Gift packaging.
            var giftFeeValue = input.IsGift ? Math.Max(0, shopSetting.GiftFee) : 0;

            // Delivery fee: free when (subtotal - discount) reaches the
            // threshold; always 0 for self-pickup orders.
            decimal feeValue = 0;
            var isPickup = fulfillment == FulfillmentMethod.Pickup;
            if (!isPickup && shopSetting.DeliveryFee > 0)
            {
                var afterDiscount = subtotal - discountValue;
                var freeShip = shopSetting.FreeShipThreshold > 0 && afterDiscount >= shopSetting.FreeShipThreshold;
                feeValue = freeShip ? 0 : shopSetting.DeliveryFee;
            }

            // Deposit: large bank-transfer orders are flagged; the checkout
            // shows "Cần đặt cọc Xđ" and the admin confirms via ConfirmPayment.
            var totalBeforeDeposit = subtotal - discountValue - loyaltyValue - tierValue + feeValue + giftFeeValue;
            var requiresDeposit = paymentMethod == PaymentMethod.BankTransfer
                && shopSetting.DepositThreshold > 0
                && totalBeforeDeposit >= shopSetting.DepositThreshold;

            var order = Order.Create(
                orderNumber,
                input.CustomerName,
                input.CustomerPhone,
                input.CustomerEmail,
                input.CustomerAddress,
                input.Notes,
                paymentMethod,
                lines,
                Money.Of(feeValue, currency),
                appliedCode,
                Money.Of(discountValue, currency),
                fulfillment,
                Money.Of(giftFeeValue, currency),
                taxCode,
                companyName,
                recipientName,
                recipientPhone,
                input.IsGift,
                input.GiftMessage,
                requiresDeposit,
                Money.Of(loyaltyValue, currency),
                redeemedPoints,
                Money.Of(tierValue, currency),
                referralCodeUsed);

            await _orders.InsertAsync(order);

            // Zalo OA e-receipt (stub: logs "would send" until configured).
            await _zalo.SendOrderReceiptAsync(
                order.OrderNumber, order.CustomerPhone, order.TotalAmount.Amount);

            // Order-confirmation email (order creation only, never on payment
            // confirmation). Only when the customer gave an email address.
            // The row is queued via the existing EmailMessage pipeline; with
            // the current Fake/test provider nothing is actually delivered.
            if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                var (subject, body) = OrderConfirmationEmailComposer.Compose(order, _bankTransfer);
                await _emailMessages.AddAsync(new EmailMessage
                {
                    From = OrderConfirmationEmailComposer.FromAddress,
                    Tos = order.CustomerEmail,
                    Subject = subject,
                    Body = body
                });

                // Same explicit-save pattern as the Product*EventHandlers:
                // the template repository only stages the row in the DbSet.
                await _emailMessages.UnitOfWork.SaveChangesAsync();
            }

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<PagedResultDto<OrderDto>> GetListAsync(GetOrdersInput input)
        {
            var queryable = await _orders.GetQueryableAsync();
            System.Linq.IQueryable<Order> query = queryable.OrderByDescending(x => x.CreatedAt);

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.Trim();
                query = query.Where(x =>
                    x.OrderNumber.Contains(term)
                    || x.CustomerName.Contains(term)
                    || x.CustomerPhone.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(input.Status)
                && Enum.TryParse<OrderStatus>(input.Status, ignoreCase: true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            return new PagedResultDto<OrderDto>(
                totalCount,
                _objectMapper.Map<List<Order>, List<OrderDto>>(items));
        }

        public async Task<OrderDto> GetAsync(Guid id)
        {
            var order = await FindOrThrowAsync(id);
            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto input)
        {
            var order = await FindOrThrowAsync(id);

            if (!Enum.TryParse<OrderStatus>(input.Status, ignoreCase: true, out var status))
            {
                throw new Domain.Exceptions.DomainException($"Unknown order status '{input.Status}'.");
            }

            ApplyStatus(order, status);
            await _orders.UpdateAsync(order);

            if (status == OrderStatus.Completed)
            {
                // Earn loyalty points + referral bonuses (once per order —
                // Complete() can only run a single time per order).
                await _loyalty.AwardCompletedOrderAsync(order);
            }

            if (status == OrderStatus.Cancelled)
            {
                await _loyalty.RefundRedeemedPointsAsync(order);
            }

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<OrderDto> CancelByCustomerAsync(CancelOrderByCustomerInput input)
        {
            var order = await FindByNumberAndPhoneAsync(input.OrderNumber, input.CustomerPhone);

            if (order.Status != OrderStatus.Pending)
            {
                throw new Domain.Exceptions.DomainException(
                    "Chỉ đơn hàng đang chờ xác nhận mới có thể hủy.");
            }

            order.Cancel();
            await _orders.UpdateAsync(order);
            await _loyalty.RefundRedeemedPointsAsync(order);

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<OrderDto> UpdateLinesByCustomerAsync(UpdateOrderLinesInput input)
        {
            var order = await FindByNumberAndPhoneAsync(input.OrderNumber, input.CustomerPhone);

            order.UpdateLineQuantities(
                input.Lines.Select(x => (x.LineId, x.Quantity)));

            // Re-check the minimum order amount against the new subtotal.
            var shopSetting = await _shopSettings.GetAsync();
            var subtotal = order.Lines.Sum(x => x.LineTotal.Amount);
            if (shopSetting.MinOrderAmount > 0 && subtotal < shopSetting.MinOrderAmount)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Đơn hàng tối thiểu {shopSetting.MinOrderAmount:N0} ₫. Vui lòng thêm món vào giỏ.");
            }

            await _orders.UpdateAsync(order);
            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<OrderReviewDto> SubmitReviewAsync(SubmitReviewInput input)
        {
            var order = await FindByNumberAndPhoneAsync(input.OrderNumber, input.CustomerPhone);

            if (order.Status != OrderStatus.Completed)
            {
                throw new Domain.Exceptions.DomainException(
                    "Chỉ đánh giá được đơn hàng đã hoàn thành.");
            }

            var reviewQueryable = await _reviews.GetQueryableAsync();
            var existing = await _asyncExecuter.FirstOrDefaultAsync(
                reviewQueryable.Where(x => x.OrderId == order.Id));
            if (existing != null)
            {
                throw new Domain.Exceptions.DomainException("Đơn hàng này đã được đánh giá.");
            }

            var review = new OrderReview
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerPhone = order.CustomerPhone,
                Rating = input.Rating,
                Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            await _reviews.InsertAsync(review, autoSave: true);

            return MapReviewToDto(review);
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<List<OrderReviewDto>> GetReviewsAsync()
        {
            var reviewQueryable = await _reviews.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                reviewQueryable.OrderByDescending(x => x.CreatedAt).Take(200));
            return items.Select(MapReviewToDto).ToList();
        }

        /// <summary>
        /// Post-checkout lucky spin. The server picks the segment (weighted)
        /// and generates a real single-use voucher tied to the phone; the
        /// frontend only animates the wheel to the returned segment.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<SpinResultDto> SpinAsync(SpinInput input)
        {
            var order = await _orders.FindAsync(input.OrderId);
            if (order == null || !PhonesMatch(order.CustomerPhone, input.CustomerPhone))
            {
                throw new OrderNotFoundException("Order not found. Please check the order number and phone.");
            }

            if (order.LuckySpinUsed)
            {
                throw new Domain.Exceptions.DomainException("Lượt quay may mắn đã được sử dụng.");
            }

            order.MarkLuckySpinUsed();
            await _orders.UpdateAsync(order);

            var segment = PickSpinSegment();
            var settings = await _shopSettings.GetAsync();
            var phone = _loyalty.NormalizePhone(input.CustomerPhone);
            var expiresAt = DateTime.UtcNow.AddDays(30);

            string code = null;
            string prizeLabel;
            switch (segment)
            {
                case "Percent5":
                    prizeLabel = "Giảm 5%";
                    code = (await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                    {
                        Prefix = "SPIN",
                        DiscountPercent = 5,
                        ExpiresAt = expiresAt,
                        OwnerPhone = phone
                    })).Code;
                    break;
                case "Percent10":
                    prizeLabel = "Giảm 10%";
                    code = (await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                    {
                        Prefix = "SPIN",
                        DiscountPercent = 10,
                        ExpiresAt = expiresAt,
                        OwnerPhone = phone
                    })).Code;
                    break;
                case "Percent15":
                    prizeLabel = "Giảm 15%";
                    code = (await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                    {
                        Prefix = "SPIN",
                        DiscountPercent = 15,
                        ExpiresAt = expiresAt,
                        OwnerPhone = phone
                    })).Code;
                    break;
                case "FreeShip":
                    prizeLabel = "Miễn phí ship";
                    code = (await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                    {
                        Prefix = "FREESHIP",
                        DiscountAmount = settings.DeliveryFee > 0 ? settings.DeliveryFee : 15000,
                        ExpiresAt = expiresAt,
                        OwnerPhone = phone
                    })).Code;
                    break;
                case "Cash20k":
                    prizeLabel = "Giảm 20.000đ";
                    code = (await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
                    {
                        Prefix = "SPIN",
                        DiscountAmount = 20000,
                        ExpiresAt = expiresAt,
                        OwnerPhone = phone
                    })).Code;
                    break;
                default:
                    prizeLabel = "Chúc may mắn lần sau!";
                    break;
            }

            return new SpinResultDto
            {
                Segment = segment,
                PrizeLabel = prizeLabel,
                Code = code,
                ExpiresAt = code == null ? (DateTime?)null : expiresAt
            };
        }

        /// <summary>
        /// Cancels Pending bank-transfer orders that stayed unpaid past the
        /// configured expiry (worker entry point).
        /// </summary>
        public async Task<int> AutoCancelExpiredBankTransfersAsync()
        {
            var settings = await _shopSettings.GetAsync();
            var expiryMinutes = settings.BankTransferExpiryMinutes < 1
                ? 30
                : settings.BankTransferExpiryMinutes;
            var cutoff = DateTime.UtcNow.AddMinutes(-expiryMinutes);

            var queryable = await _orders.GetQueryableAsync();
            var expired = await _asyncExecuter.ToListAsync(
                queryable.Where(x =>
                    x.Status == OrderStatus.Pending
                    && x.PaymentMethod == PaymentMethod.BankTransfer
                    && x.PaymentStatus == PaymentStatus.Unpaid
                    && x.CreatedAt < cutoff));

            foreach (var order in expired)
            {
                order.Cancel();
                await _orders.UpdateAsync(order);
                await _loyalty.RefundRedeemedPointsAsync(order);
            }

            return expired.Count;
        }

        /// <summary>
        /// Public lookup that also proves ownership via phone match.
        /// </summary>
        private async Task<Order> FindByNumberAndPhoneAsync(string orderNumber, string customerPhone)
        {
            var order = await _orders.GetByOrderNumberAsync(orderNumber?.Trim());
            if (order == null || !PhonesMatch(order.CustomerPhone, customerPhone))
            {
                throw new OrderNotFoundException("Order not found. Please check the order number and phone.");
            }

            return order;
        }

        private static OrderReviewDto MapReviewToDto(OrderReview review)
        {
            return new OrderReviewDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                OrderNumber = review.OrderNumber,
                CustomerPhone = review.CustomerPhone,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }

        /// <summary>Weighted random pick of the lucky-spin segment.</summary>
        private static string PickSpinSegment()
        {
            var segments = new (string Key, int Weight)[]
            {
                ("Percent5", 25),
                ("Percent10", 20),
                ("Percent15", 12),
                ("FreeShip", 15),
                ("Cash20k", 18),
                ("Miss", 10)
            };

            var total = segments.Sum(x => x.Weight);
            var roll = new Random().Next(total);
            var acc = 0;
            foreach (var (key, weight) in segments)
            {
                acc += weight;
                if (roll < acc)
                {
                    return key;
                }
            }

            return "Miss";
        }

        public async Task<OrderDto> MarkTransferredAsync(Guid id, MarkTransferredDto input)
        {
            var order = await FindOrThrowAsync(id);

            if (!PhonesMatch(order.CustomerPhone, input.CustomerPhone))
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại không khớp với đơn hàng.");
            }

            order.MarkTransferred();
            await _orders.UpdateAsync(order);

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<OrderDto> ConfirmPaymentAsync(Guid id)
        {
            var order = await FindOrThrowAsync(id);
            order.ConfirmPayment();
            await _orders.UpdateAsync(order);

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public async Task<MoMoCreatePaymentResultDto> CreateMoMoPaymentAsync(Guid id)
        {
            var order = await FindOrThrowAsync(id);
            return await _momo.CreatePaymentAsync(order);
        }

        public async Task<OrderDto> TrackOrderAsync(TrackOrderInput input)
        {
            var order = await _orders.GetByOrderNumberAsync(input.OrderNumber?.Trim());
            if (order == null || !PhonesMatch(order.CustomerPhone, input.CustomerPhone))
            {
                // Same error for "not found" and "phone mismatch" so the
                // endpoint cannot be used to probe order numbers.
                throw new OrderNotFoundException("Order not found. Please check the order number and phone.");
            }

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        /// <summary>
        /// Admin: customers aggregated by phone number (order count, total
        /// spent, last order). Cancelled orders are excluded from the totals.
        /// </summary>
        public async Task<List<CustomerDto>> GetCustomersAsync(string searchTerm)
        {
            var orderQueryable = await _orders.GetQueryableAsync();
            var query = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(x =>
                    x.CustomerPhone.Contains(term) || x.CustomerName.Contains(term));
            }

            return await _asyncExecuter.ToListAsync(
                query.GroupBy(x => x.CustomerPhone)
                    .Select(g => new CustomerDto
                    {
                        CustomerPhone = g.Key,
                        CustomerName = g.Max(x => x.CustomerName),
                        OrderCount = g.LongCount(),
                        TotalSpent = g.Sum(x => x.TotalAmount.Amount),
                        LastOrderAt = g.Max(x => x.CreatedAt)
                    })
                    .OrderByDescending(x => x.TotalSpent));
        }

        /// <summary>
        /// Public: last order info for a phone number — only the customer
        /// name and delivery address (no order details, no sensitive data),
        /// so the checkout form can offer one-click address re-fill.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<LastOrderInfoDto> GetLastOrderInfoByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return null;
            }

            var normalized = new string(phone.Where(char.IsDigit).ToArray());
            if (normalized.Length < 9)
            {
                return null;
            }

            // Phone matching reuses the tolerant PhonesMatch logic; it runs
            // in memory over recent orders to keep the query provider-agnostic.
            var orderQueryable = await _orders.GetQueryableAsync();
            var recent = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && !string.IsNullOrWhiteSpace(x.CustomerAddress))
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(50));

            var match = recent.FirstOrDefault(x => PhonesMatch(x.CustomerPhone, phone));
            if (match == null)
            {
                return null;
            }

            return new LastOrderInfoDto
            {
                CustomerName = match.CustomerName,
                CustomerAddress = match.CustomerAddress,
                LastOrderAt = match.CreatedAt
            };
        }

        private async Task<Order> FindOrThrowAsync(Guid id)
        {
            var order = await _orders.FindAsync(id);
            if (order == null)
            {
                throw new OrderNotFoundException(id);
            }

            return order;
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var random = new Random();

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var orderNumber = $"PX-{DateTime.UtcNow:yyyyMMdd}-{random.Next(1000, 9999)}";

                if (!await _orders.ExistsWithOrderNumberAsync(orderNumber))
                {
                    return orderNumber;
                }
            }

            throw new Domain.Exceptions.DomainException("Could not generate a unique order number. Please try again.");
        }

        /// <summary>
        /// Phone comparison tolerant of spaces/dashes/dots and a leading
        /// country code (e.g. +84 vs 0). Both sides are reduced to digits,
        /// then the last 9 digits are compared.
        /// </summary>
        internal static bool PhonesMatch(string orderPhone, string givenPhone)
        {
            if (string.IsNullOrWhiteSpace(orderPhone) || string.IsNullOrWhiteSpace(givenPhone))
            {
                return false;
            }

            var a = new string(orderPhone.Where(char.IsDigit).ToArray());
            var b = new string(givenPhone.Where(char.IsDigit).ToArray());

            if (a.Length < 9 || b.Length < 9)
            {
                return string.Equals(a, b, StringComparison.Ordinal);
            }

            return string.Equals(
                a.Substring(a.Length - 9),
                b.Substring(b.Length - 9),
                StringComparison.Ordinal);
        }

        private static void ApplyStatus(Order order, OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.Confirmed:
                    order.Confirm();
                    break;
                case OrderStatus.Preparing:
                    order.StartPreparing();
                    break;
                case OrderStatus.Delivering:
                    order.StartDelivering();
                    break;
                case OrderStatus.Completed:
                    order.Complete();
                    break;
                case OrderStatus.Cancelled:
                    order.Cancel();
                    break;
                case OrderStatus.Pending:
                    throw new Domain.Exceptions.DomainException("Cannot move an order back to 'Pending'.");
                default:
                    throw new Domain.Exceptions.DomainException($"Unsupported order status '{status}'.");
            }
        }
    }
}
