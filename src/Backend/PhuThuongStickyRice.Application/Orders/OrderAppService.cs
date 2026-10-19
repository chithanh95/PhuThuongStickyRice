using PhuThuongStickyRice.Application.Orders.DTOs;
using PhuThuongStickyRice.Application.Orders.Email;
using PhuThuongStickyRice.Application.Orders.Exceptions;
using PhuThuongStickyRice.Application.Orders.Payments;
using PhuThuongStickyRice.Application.Permissions;
using PhuThuongStickyRice.Application.Products.Exceptions;
using PhuThuongStickyRice.Application.Shop;
using PhuThuongStickyRice.Application.Shop.DTOs;
using PhuThuongStickyRice.Application.Staff;
using PhuThuongStickyRice.Application.Discounts;
using PhuThuongStickyRice.Application.Discounts.DTOs;
using PhuThuongStickyRice.Application.Loyalty;
using PhuThuongStickyRice.Application.Loyalty.DTOs;
using PhuThuongStickyRice.Application.Customer;
using PhuThuongStickyRice.Application.Zalo;

using PhuThuongStickyRice.Domain.Entities;
using PhuThuongStickyRice.Domain.Repositories;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Linq;
using Volo.Abp.ObjectMapping;
using PhuThuongStickyRice.Application.Common;
using PhuThuongStickyRice.Application.Orders;
using PhuThuongStickyRice.Application.Security;

namespace PhuThuongStickyRice.Application.Orders
{
    public interface IOrderAppService : IApplicationService
    {
        /// <summary>
        /// Item 96: reCAPTCHA chỉ verify cho checkout công khai (đi qua HTTP,
        /// có token trình duyệt). Các luồng nội bộ (POS staff, Messenger bot,
        /// chốt gom đơn) KHÔNG có token → truyền skipRecaptchaCheck: true,
        /// nếu không sẽ throw "Xác thực chống spam không thành công" khi admin
        /// bật reCAPTCHA và các luồng này gãy hoàn toàn.
        /// </summary>
        Task<OrderDto> CreateAsync(CreateOrderDto input, bool skipRecaptchaCheck = false);

        Task<PagedResultDto<OrderDto>> GetListAsync(GetOrdersInput input);

        Task<OrderDto> GetAsync(Guid id);

        Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto input);

        /// <summary>Admin: assigns (or unassigns with null) a delivery shipper.</summary>
        Task<OrderDto> AssignShipperAsync(Guid id, AssignShipperDto input);

        /// <summary>Admin (kitchen display): orders in Pending/Confirmed/Preparing, oldest first.</summary>
        Task<List<OrderDto>> GetKitchenOrdersAsync();

        /// <summary>Admin (kitchen display): advance one kitchen stage
        /// (Pending→Confirmed→Preparing→Delivering).</summary>
        Task<OrderDto> AdvanceKitchenStatusAsync(Guid id);

        /// <summary>Admin: hide/unhide an order review.</summary>
        Task<OrderReviewDto> SetReviewHiddenAsync(Guid id, SetReviewHiddenDto input);

        /// <summary>Admin: reply to an order review (shown publicly).</summary>
        Task<OrderReviewDto> ReplyReviewAsync(Guid id, ReplyReviewDto input);

        /// <summary>Item 52: customer rates the shipper (1-5) for an order review.
        /// Ownership is verified via CustomerPhone (same no-login pattern as order tracking).</summary>
        Task<OrderReviewDto> SetShipperRatingAsync(Guid id, SetShipperRatingDto input);

        /// <summary>Public: non-hidden reviews for the landing page (masked phone, no voucher code).</summary>
        Task<List<PublicOrderReviewDto>> GetPublicReviewsAsync();

        /// <summary>Customer marks their bank-transfer order as transferred.</summary>
        Task<OrderDto> MarkTransferredAsync(Guid id, MarkTransferredDto input);

        /// <summary>Public: order history lookup by phone number.</summary>
        Task<List<OrderDto>> GetOrdersByPhoneAsync(string phone);

        /// <summary>Admin confirms a payment (bank transfer).</summary>
        Task<OrderDto> ConfirmPaymentAsync(Guid id);

        /// <summary>Creates a MoMo payment for a MoMo order (test simulator or real payUrl).</summary>
        Task<MoMoCreatePaymentResultDto> CreateMoMoPaymentAsync(Guid id);

        /// <summary>Public order lookup by order number + phone.</summary>
        Task<OrderDto> TrackOrderAsync(TrackOrderInput input);

        /// <summary>Admin: customers aggregated by phone number (paginated).</summary>
        Task<List<CustomerDto>> GetCustomersAsync(string searchTerm, int skip = 0, int take = 50);

        /// <summary>Item 131: khách quá N ngày không có đơn (để winback).</summary>
        Task<List<CustomerDto>> GetInactiveCustomersAsync(int days, int take = 50);

        /// <summary>
        /// Public: last order info for a phone number (name + address only),
        /// used to auto-fill the checkout form.
        /// </summary>
        Task<LastOrderInfoDto> GetLastOrderInfoByPhoneAsync(string phone);

        /// <summary>
        /// Item 143 (Public): true when the phone placed at least one order.
        /// </summary>
        Task<bool> HasOrderedBeforeAsync(string phone);

        /// <summary>
        /// Item 143 (Public): first-order voucher for the phone. Returns
        /// IsNewCustomer=false when the phone already ordered; otherwise the
        /// existing unused CHAO- code, or a freshly generated one. Throws
        /// DomainException on an invalid phone.
        /// </summary>
        Task<FirstOrderVoucherDto> GetFirstOrderVoucherAsync(string phone);

        /// <summary>Customer self-service: cancel a Pending order (phone must match).</summary>
        Task<OrderDto> CancelByCustomerAsync(CancelOrderByCustomerInput input);

        /// <summary>
        /// Wave 6c (item 33): admin sets/clears the internal order note.
        /// Never exposed on public endpoints.
        /// </summary>
        Task<OrderDto> UpdateAdminNoteAsync(Guid id, UpdateAdminNoteDto input);

        /// <summary>
        /// Staff confirms a ShipperPickup order was collected at the shop
        /// ("Xác nhận đã lấy hàng"). Optionally records the customer-booked
        /// shipper info.
        /// </summary>
        Task<OrderDto> MarkPickedUpAsync(Guid id, MarkPickedUpDto input);

        /// <summary>
        /// Item 163: staff marks cash as collected for an order
        /// ("💵 Đã thu tiền"). Rejected for cancelled or already-collected
        /// orders.
        /// </summary>
        Task<OrderDto> MarkCashCollectedAsync(Guid id);

        /// <summary>
        /// Wave 6c (item 34): status-transition timeline of an order.
        /// </summary>
        Task<List<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id);

        /// <summary>Customer self-service: edit quantities of a Pending order (phone must match).</summary>
        Task<OrderDto> UpdateLinesByCustomerAsync(UpdateOrderLinesInput input);

        /// <summary>Customer self-service: change address / delivery slot of a Pending order (phone must match).</summary>
        Task<OrderDto> UpdateAddressByCustomerAsync(UpdateOrderAddressByCustomerInput input);

        /// <summary>Item 138: customer self-service — update recipient phone + notes of a Pending order.</summary>
        Task<OrderDto> UpdateContactByCustomerAsync(UpdateOrderContactByCustomerInput input);

        /// <summary>
        /// Item 142 (Admin): groups of possibly-duplicate orders — same
        /// phone, placed within the last 30 minutes, still Pending/Confirmed.
        /// </summary>
        Task<List<DuplicateOrderGroupDto>> GetDuplicateOrderGroupsAsync();

        /// <summary>
        /// Item 142 (Admin/Cashier): merges the given orders' lines into the
        /// primary order and cancels the merged ones with a note.
        /// Always explicit — never automatic.
        /// </summary>
        Task<OrderDto> MergeOrdersAsync(MergeOrdersInput input);

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
        // Lucky-spin prize code generation.
        private const string SpinCodePrefix = "SPIN";
        private const string SpinFreeshipCodePrefix = "FREESHIP";
        private const int SpinCodeValidityDays = 30;
        private const decimal SpinFreeshipFallbackAmount = 15000m;

        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly IAsyncQueryableExecuter _asyncExecuter;
        private readonly IObjectMapper _objectMapper;
        private readonly IEmailMessageRepository _emailMessages;
        private readonly IPaymentConfigProvider _paymentConfig;
        private readonly IMoMoPaymentService _momo;
        private readonly IShopSettingAppService _shopSettings;
        private readonly IDiscountAppService _discounts;
        private readonly Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> _sizeOptions;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> _toppings;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Combo, Guid> _combos;
        private readonly ILoyaltyAppService _loyalty;
        private readonly IZaloNotificationService _zalo;
        private readonly Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> _reviews;
        private readonly IBlacklistAppService _blacklist;
        private readonly IPromotionAppService _promotions;
        private readonly IRecaptchaService _recaptcha;
        private readonly IRewardRuleAppService _rewardRules;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Shipper, Guid> _shippers;
        private readonly IWebhookDispatcher _webhooks;
        private readonly Volo.Abp.Domain.Repositories.IRepository<Branch, Guid> _branches;
        private readonly IWalletAppService _wallets;
        private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;

        /// <summary>Wave 6c (item 34): status-transition audit trail.</summary>
        private readonly Volo.Abp.Domain.Repositories.IRepository<OrderStatusHistory, Guid> _statusHistory;
        private readonly IPermissionService _permissions;
        private readonly IStaffActivityLogger _activityLogger;

        /// <summary>Item 149: customer tags (shown on customer list + order detail).</summary>
        private readonly ICustomerTagAppService _customerTags;
        private readonly IVipPriorityAppService _vipPriority;

        /// <summary>perf-hunt 2026-10-19: cache for public review listing (landing hot path).</summary>
        private readonly Products.IProductCatalogCache _catalogCache;

        public OrderAppService(
            IOrderRepository orders,
            IProductRepository products,
            IAsyncQueryableExecuter asyncExecuter,
            IObjectMapper objectMapper,
            IEmailMessageRepository emailMessages,
            IPaymentConfigProvider paymentConfig,
            IMoMoPaymentService momo,
            IShopSettingAppService shopSettings,
            IDiscountAppService discounts,
            Volo.Abp.Domain.Repositories.IRepository<ProductSizeOption, Guid> sizeOptions,
            Volo.Abp.Domain.Repositories.IRepository<Topping, Guid> toppings,
            Volo.Abp.Domain.Repositories.IRepository<Combo, Guid> combos,
            ILoyaltyAppService loyalty,
            IZaloNotificationService zalo,
            Volo.Abp.Domain.Repositories.IRepository<OrderReview, Guid> reviews,
            IBlacklistAppService blacklist,
            IPromotionAppService promotions,
            IRecaptchaService recaptcha,
            IRewardRuleAppService rewardRules,
            Volo.Abp.Domain.Repositories.IRepository<Shipper, Guid> shippers,
            IWebhookDispatcher webhooks,
            Volo.Abp.Domain.Repositories.IRepository<Branch, Guid> branches,
            IWalletAppService wallets,
            Volo.Abp.Domain.Repositories.IRepository<OrderStatusHistory, Guid> statusHistory,
            ICustomerTagAppService customerTags,
            IVipPriorityAppService vipPriority,
            IPermissionService permissions,
            IStaffActivityLogger activityLogger,
            System.Net.Http.IHttpClientFactory httpClientFactory,
            Products.IProductCatalogCache catalogCache)
        {
            _orders = orders;
            _products = products;
            _asyncExecuter = asyncExecuter;
            _objectMapper = objectMapper;
            _emailMessages = emailMessages;
            _paymentConfig = paymentConfig;
            _momo = momo;
            _shopSettings = shopSettings;
            _discounts = discounts;
            _sizeOptions = sizeOptions;
            _toppings = toppings;
            _combos = combos;
            _loyalty = loyalty;
            _zalo = zalo;
            _reviews = reviews;
            _blacklist = blacklist;
            _promotions = promotions;
            _recaptcha = recaptcha;
            _rewardRules = rewardRules;
            _shippers = shippers;
            _webhooks = webhooks;
            _branches = branches;
            _wallets = wallets;
            _statusHistory = statusHistory;
            _customerTags = customerTags;
            _vipPriority = vipPriority;
            _permissions = permissions;
            _activityLogger = activityLogger;
            _httpClientFactory = httpClientFactory;
            _catalogCache = catalogCache;
        }

        public virtual async Task<OrderDto> CreateAsync(CreateOrderDto input, bool skipRecaptchaCheck = false)
        {
            if (input.Lines == null || input.Lines.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Order must contain at least one line.");
            }

            // Wave 6b (item 30): pause toggle + open-hours gating. Checked
            // first so a paused/closed shop rejects before any other work.
            // Item 117: holiday-aware — a full-day holiday auto-pauses ordering.
            var shopSetting = await _shopSettings.GetPublicAsync();
            EnsureAcceptingOrders(shopSetting);

            // Item 97: idempotency — the same key retried within 24h returns
            // the existing order instead of creating a duplicate.
            var idempotencyKey = string.IsNullOrWhiteSpace(input.IdempotencyKey)
                ? null
                : input.IdempotencyKey.Trim();
            if (idempotencyKey != null)
            {
                var duplicate = await FindByIdempotencyKeyAsync(idempotencyKey);
                if (duplicate != null)
                {
                    var duplicateDto = _objectMapper.Map<Order, OrderDto>(duplicate);
                    await FillShipperNamesAsync(new[] { duplicateDto });
                    return duplicateDto;
                }
            }

            // Item 88: blocked phones cannot place orders.
            if (await _blacklist.IsBlacklistedAsync(input.CustomerPhone))
            {
                throw new Domain.Exceptions.DomainException(
                    "Số điện thoại này tạm thời không thể đặt hàng.");
            }

            // Wave 6d (item 39): anti-spam — cap non-cancelled orders per
            // phone per Vietnam day. Checked before anything is created.
            await EnsurePhoneDailyLimitAsync(shopSetting.MaxOrdersPerPhonePerDay, input.CustomerPhone);

            // Item 96: reCAPTCHA v3, verified server-side when configured.
            // Internal flows (POS, Messenger bot, group-order close) call with
            // skipRecaptchaCheck: true — they have no browser token.
            if (!skipRecaptchaCheck && !await _recaptcha.VerifyAsync(input.RecaptchaToken))
            {
                throw new Domain.Exceptions.DomainException(
                    "Xác thực chống spam không thành công. Vui lòng thử lại.");
            }

            if ((!Enum.TryParse<PaymentMethod>(input.PaymentMethod, ignoreCase: true, out var paymentMethod)
                    || !Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
                && !string.IsNullOrWhiteSpace(input.PaymentMethod))
            {
                throw new Domain.Exceptions.DomainException($"Unknown payment method '{input.PaymentMethod}'.");
            }

            // Item 50: pay-later requires at least 5 completed orders from this phone.
            if (paymentMethod == PaymentMethod.PayLater)
            {
                var phone = (input.CustomerPhone ?? string.Empty).Trim();
                var orderQueryable = await _orders.GetQueryableAsync();
                var completedCount = await _asyncExecuter.CountAsync(
                    orderQueryable.Where(o => o.CustomerPhone == phone && o.Status == OrderStatus.Completed));
                if (completedCount < 5)
                {
                    throw new Domain.Exceptions.DomainException(
                        "Trả sau chỉ áp dụng cho khách đã có ít nhất 5 đơn hoàn thành.");
                }
            }

            // Fulfillment: customer-booked shipper pickup (default — the shop
            // does not deliver), self-pickup, or legacy home delivery.
            var fulfillment = FulfillmentMethod.ShipperPickup;
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
                string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo, string Note)>();
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
                        true,
                        string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim()));
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

                // Item 164: sale time windows (and seasonal window), evaluated
                // against the scheduled pickup time for pre-orders, now for
                // immediate orders.
                if (!product.IsAvailableNow(GetAvailabilityCheckTime(input)))
                {
                    throw new Domain.Exceptions.DomainException(
                        $"Món '{product.Name}' không bán trong khung giờ này.");
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
                    sizeName, toppingNames, toppingsExtra, false,
                    string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim()));
            }

            // Item 165: daily portion limit per product. Counted server-side
            // from today's non-cancelled orders. Best-effort against races
            // (no serializable transaction); the overshoot window is tiny.
            await EnsureDailyLimitsAsync(lines);

            var orderNumber = await GenerateOrderNumberAsync();

            var subtotal = lines.Sum(x => x.Quantity * x.UnitPrice.Amount);
            var currency = lines.First().UnitPrice.Currency;

            // Item 91: auto-apply the best active "giờ vàng" promotion
            // (Vietnam local time). It comes first in the discount priority
            // order, so the subtotal below already accounts for it.
            string promotionName = null;
            decimal promotionDiscount = 0;
            var bestPromo = await _promotions.GetBestActiveAsync();
            if (bestPromo != null && bestPromo.DiscountPercent > 0)
            {
                promotionName = bestPromo.Name;
                promotionDiscount = Math.Round(subtotal * bestPromo.DiscountPercent / 100, 0);
            }

            // Minimum order amount (shop setting, loaded once at the top of CreateAsync).

            // Item 105: feature flags for the checkout extras (UI hides them
            // too; the backend is the real gate).
            var flags = LazyServiceProvider.LazyGetRequiredService<IFeatureFlagAppService>();
            var giftWrapEnabled = await flags.IsEnabledAsync(FeatureFlag.GiftWrap);
            var referralsEnabled = await flags.IsEnabledAsync(FeatureFlag.Referrals);

            if (shopSetting.MinOrderAmount > 0 && subtotal < shopSetting.MinOrderAmount)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Đơn hàng tối thiểu {shopSetting.MinOrderAmount:N0} ₫. Vui lòng thêm món vào giỏ.");
            }

            // Discount code (validated + usage counted here; owner-tied
            // spin/birthday codes require the matching phone). The code
            // applies to the subtotal remaining after the happy-hour promotion.
            decimal discountValue = 0;
            string appliedCode = null;
            bool isFreeship = false;
            if (!string.IsNullOrWhiteSpace(input.DiscountCode))
            {
                // Validate only here — usage is bumped after the order row
                // is written (see below), so a failed order never burns a use.
                discountValue = await _discounts.ValidateAndComputeAsync(
                    input.DiscountCode, subtotal - promotionDiscount, input.CustomerPhone);
                appliedCode = input.DiscountCode.Trim().ToUpperInvariant();

                // Item 120: freeship vouchers zero the delivery fee instead
                // of reducing the subtotal.
                isFreeship = await _discounts.IsFreeshipAsync(
                    input.DiscountCode, subtotal - promotionDiscount, input.CustomerPhone);
            }

            // Loyalty account (created on first order) + tier discount.
            var loyaltyAccount = await _loyalty.GetOrCreateAsync(input.CustomerPhone);
            var (_, tierPercent) = _loyalty.GetTier(loyaltyAccount.TotalSpent, shopSetting);
            decimal tierValue = 0;
            if (loyaltyAccount.TotalSpent > 0 && tierPercent > 0)
            {
                tierValue = Math.Round((subtotal - promotionDiscount - discountValue) * tierPercent / 100, 0);
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
            // Item 105: referrals are feature-flagged.
            string referralCodeUsed = null;
            var referralCode = referralsEnabled ? input.ReferralCode : null;
            if (!string.IsNullOrWhiteSpace(referralCode))
            {
                var referrer = await _loyalty.FindByReferralCodeAsync(referralCode);
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

            var isGift = giftWrapEnabled && input.IsGift;

            // Gift packaging.
            var giftFeeValue = isGift ? Math.Max(0, shopSetting.GiftFee) : 0;

            // Delivery fee: the shop does not deliver — always 0 for
            // ShipperPickup and self-pickup orders; legacy Delivery orders
            // keep the old threshold/freeship rules.
            decimal feeValue = 0;
            var isPickup = fulfillment == FulfillmentMethod.Pickup
                || fulfillment == FulfillmentMethod.ShipperPickup;
            if (!isPickup && shopSetting.DeliveryFee > 0 && !isFreeship)
            {
                var afterDiscount = subtotal - promotionDiscount - discountValue;
                var freeShip = shopSetting.FreeShipThreshold > 0 && afterDiscount >= shopSetting.FreeShipThreshold;
                feeValue = freeShip ? 0 : shopSetting.DeliveryFee;
            }

            // Deposit: large bank-transfer orders are flagged; the checkout
            // shows "Cần đặt cọc Xđ" and the admin confirms via ConfirmPayment.
            var totalBeforeDeposit = subtotal - promotionDiscount - discountValue - loyaltyValue - tierValue + feeValue + giftFeeValue;
            var requiresDeposit = paymentMethod == PaymentMethod.BankTransfer
                && shopSetting.DepositThreshold > 0
                && totalBeforeDeposit >= shopSetting.DepositThreshold;

            // Item 109: branch (nullable). Only active branches are
            // accepted; the branch name is denormalized onto the order.
            Guid? branchId = input.BranchId;
            string branchName = null;
            if (branchId.HasValue)
            {
                var branch = await _branches.FindAsync(branchId.Value);
                if (branch == null || !branch.IsActive)
                {
                    throw new Domain.Exceptions.DomainException("Chi nhánh không tồn tại hoặc đã ngừng hoạt động.");
                }

                branchName = branch.Name;
            }

            // Pre-order (đặt trước): ScheduledFor must be at least 1 hour
            // ahead and inside shop open hours. Null = nhận ngay.
            DateTime? scheduledFor = null;
            if (input.ScheduledFor.HasValue)
            {
                var sf = input.ScheduledFor.Value;
                // Accept both UTC and unspecified/local input; normalize to UTC.
                if (sf.Kind == DateTimeKind.Unspecified)
                {
                    sf = DateTime.SpecifyKind(sf, DateTimeKind.Local).ToUniversalTime();
                }
                else if (sf.Kind == DateTimeKind.Local)
                {
                    sf = sf.ToUniversalTime();
                }

                if (sf <= DateTime.UtcNow.AddHours(1))
                {
                    throw new Domain.Exceptions.DomainException("Giờ đặt trước phải sau thời điểm hiện tại ít nhất 1 tiếng.");
                }

                // Within open hours (Vietnam local, UTC+7).
                var vnTime = sf.AddHours(7).TimeOfDay;
                if (!TimeSpan.TryParse(shopSetting.OpenTime, out var open))
                {
                    open = TimeSpan.FromHours(6);
                }
                if (!TimeSpan.TryParse(shopSetting.CloseTime, out var close))
                {
                    close = new TimeSpan(10, 30, 0);
                }
                if (vnTime < open || vnTime >= close)
                {
                    throw new Domain.Exceptions.DomainException(
                        $"Giờ đặt trước phải trong giờ mở cửa ({shopSetting.OpenTime}–{shopSetting.CloseTime}).");
                }

                scheduledFor = sf;
            }

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
                isGift,
                input.GiftMessage,
                requiresDeposit,
                Money.Of(loyaltyValue, currency),
                redeemedPoints,
                Money.Of(tierValue, currency),
                referralCodeUsed,
                idempotencyKey,
                promotionName,
                promotionDiscount,
                branchId,
                branchName,
                input.Source,
                string.IsNullOrWhiteSpace(input.DeliverySlot) ? null : input.DeliverySlot.Trim(),
                string.IsNullOrWhiteSpace(input.PickupShipperName) ? null : input.PickupShipperName.Trim(),
                string.IsNullOrWhiteSpace(input.PickupShipperPhone) ? null : input.PickupShipperPhone.Trim(),
                scheduledFor,
                // Registered customer: link the order to their account.
                CurrentUser != null && CurrentUser.IsAuthenticated ? CurrentUser.Id : null);

            // Item 140: wallet payment — charge the prepaid balance up front;
            // insufficient balance aborts before the order row is written.
            if (order.PaymentMethod == PaymentMethod.Wallet)
            {
                await _wallets.ChargeAsync(order.CustomerPhone, order.TotalAmount.Amount, order.OrderNumber);
                order.ConfirmPayment();
            }

            await _orders.InsertAsync(order);

            // Discount usage is counted only now that the order exists.
            if (!string.IsNullOrWhiteSpace(appliedCode))
            {
                await _discounts.IncrementUsageAsync(appliedCode);
            }

            // Item 110: fire the OrderCreated webhook (fire-and-log — a
            // failing endpoint never breaks the order flow).
            await FireWebhookSafelyAsync(WebhookEndpoint.EventOrderCreated, new
            {
                order.OrderNumber,
                status = order.Status.ToString(),
                total = order.TotalAmount.Amount,
                phone = order.CustomerPhone,
                branchName
            });

            // Item 180: simple "new order" webhook (báo đơn mới) — 1 URL in
            // shop settings. Fire-and-log with 5s timeout; a failing
            // endpoint never breaks the order flow.
            await FireSimpleNewOrderWebhookAsync(order);

            // Zalo OA e-receipt (stub: logs "would send" until configured).
            await _zalo.SendOrderReceiptAsync(
                order.OrderNumber, order.CustomerPhone, order.TotalAmount.Amount);

            // Order-confirmation email (order creation only, never on payment
            // confirmation). Only when the customer gave an email address.
            // The row is queued via the existing EmailMessage pipeline; with
            // the current Fake/test provider nothing is actually delivered.
            if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
            {
                var bankTransfer = await _paymentConfig.GetBankTransferAsync();
                var (subject, body) = OrderConfirmationEmailComposer.Compose(order, bankTransfer);
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

        public virtual async Task<PagedResultDto<OrderDto>> GetListAsync(GetOrdersInput input)
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

            // Item 82: filter orders by assigned shipper.
            if (input.ShipperId.HasValue && input.ShipperId.Value != Guid.Empty)
            {
                var shipperId = input.ShipperId.Value;
                query = query.Where(x => x.ShipperId == shipperId);
            }

            // Wave 6c (item 35): date-only range on the order creation day
            // (Vietnam timezone day boundaries).
            if (input.FromDate.HasValue)
            {
                var fromUtc = VietnamTime.DayStartUtc(input.FromDate.Value);
                query = query.Where(x => x.CreatedAt >= fromUtc);
            }

            if (input.ToDate.HasValue)
            {
                var toUtc = VietnamTime.DayEndExclusiveUtc(input.ToDate.Value);
                query = query.Where(x => x.CreatedAt < toUtc);
            }

            // Item 163: filter by cash-collection state.
            if (input.CashCollected.HasValue)
            {
                var collected = input.CashCollected.Value;
                query = query.Where(x => x.CashCollected == collected);
            }

            // Item 130 (đúng): only orders whose customer is marked VIP.
            if (input.VipOnly)
            {
                var vipPhones = await _vipPriority.GetVipPhonesAsync();
                query = query.Where(x => vipPhones.Contains(x.CustomerPhone));
            }

            var totalCount = await _asyncExecuter.LongCountAsync(query);
            var items = await _asyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));

            var dtos = _objectMapper.Map<List<Order>, List<OrderDto>>(items);
            await FillShipperNamesAsync(dtos);
            await FillCustomerTagsAsync(dtos);
            return new PagedResultDto<OrderDto>(totalCount, dtos);
        }

        /// <summary>
        /// Item 149: fills <see cref="OrderDto.CustomerTags"/> for orders
        /// (single lookup, no N+1). Admin endpoints only — tags are internal.
        /// </summary>
        private async Task FillCustomerTagsAsync(IList<OrderDto> dtos)
        {
            var phones = dtos
                .Where(x => !string.IsNullOrWhiteSpace(x.CustomerPhone))
                .Select(x => x.CustomerPhone.Trim())
                .Distinct()
                .ToList();
            if (phones.Count == 0)
            {
                return;
            }

            var tagsByPhone = await _customerTags.GetTagsByPhonesAsync(phones);
            foreach (var dto in dtos)
            {
                var phone = dto.CustomerPhone?.Trim();
                if (!string.IsNullOrEmpty(phone)
                    && tagsByPhone.TryGetValue(phone, out var tags))
                {
                    dto.CustomerTags = tags;
                    // Item 130 (đúng): VIP flag for priority display/sorting.
                    dto.IsVip = tags.Any(t =>
                        string.Equals(t.TagName, "VIP", StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        /// <summary>
        /// Item 82: fills <see cref="OrderDto.ShipperName"/> for orders with an
        /// assigned shipper (single lookup, no N+1).
        /// </summary>
        private async Task FillShipperNamesAsync(IList<OrderDto> dtos)
        {
            var ids = dtos
                .Where(x => x.ShipperId.HasValue && x.ShipperId.Value != Guid.Empty)
                .Select(x => x.ShipperId.Value)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
            {
                return;
            }

            var queryable = await _shippers.GetQueryableAsync();
            var names = await _asyncExecuter.ToListAsync(
                queryable.Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, x.Name }));
            var map = names.ToDictionary(x => x.Id, x => x.Name);

            foreach (var dto in dtos)
            {
                if (dto.ShipperId.HasValue && map.TryGetValue(dto.ShipperId.Value, out var name))
                {
                    dto.ShipperName = name;
                }
            }
        }

        public virtual async Task<OrderDto> GetAsync(Guid id)
        {
            var order = await FindOrThrowAsync(id);
            var dto = _objectMapper.Map<Order, OrderDto>(order);
            await FillShipperNamesAsync(new[] { dto });
            await FillCustomerTagsAsync(new[] { dto });
            return dto;
        }

        public virtual async Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto input)
        {
            var order = await FindOrThrowAsync(id);

            if (!Enum.TryParse<OrderStatus>(input.Status, ignoreCase: true, out var status))
            {
                throw new Domain.Exceptions.DomainException($"Unknown order status '{input.Status}'.");
            }

            // Item 155: fine-grained permission — cancelling an order needs
            // orders.cancel; any other status transition needs orders.confirm.
            await _permissions.RequirePermissionAsync(
                status == OrderStatus.Cancelled ? PermissionKeys.OrdersCancel : PermissionKeys.OrdersConfirm);

            var fromStatus = order.Status;
            ApplyStatus(order, status);
            await _orders.UpdateAsync(order);

            // Wave 6c (item 34): audit the transition.
            await RecordStatusChangeAsync(order.Id, fromStatus, order.Status);

            // Item 156: staff activity log.
            var statusAction = GetStatusChangeAction(status);
            await _activityLogger.LogAsync(statusAction,
                $"Đơn {order.OrderNumber}: {fromStatus} → {order.Status}.");

            if (status == OrderStatus.Completed)
            {
                await HandleOrderCompletedAsync(order);
            }

            if (status == OrderStatus.Cancelled)
            {
                await HandleOrderCancelledAsync(order);
            }

            var updatedDto = _objectMapper.Map<Order, OrderDto>(order);
            await FillShipperNamesAsync(new[] { updatedDto });

            // Item 110: fire the OrderStatusChanged webhook (fire-and-log).
            await FireWebhookSafelyAsync(WebhookEndpoint.EventOrderStatusChanged, new
            {
                order.OrderNumber,
                status = order.Status.ToString(),
                total = order.TotalAmount.Amount,
                phone = order.CustomerPhone,
                branchName = order.BranchName
            });

            return updatedDto;
        }

        /// <summary>Item 156: maps a status transition to its staff activity log action.</summary>
        private static string GetStatusChangeAction(OrderStatus status) =>
            status == OrderStatus.Cancelled ? StaffActions.OrderCancel
            : status == OrderStatus.Confirmed ? StaffActions.OrderConfirm
            : StaffActions.OrderStatusChange;

        /// <summary>
        /// Post-completion side effects: loyalty points + referral bonuses
        /// (once per order — Complete() can only run a single time per order)
        /// and automatic reward rules (item 92).
        /// </summary>
        private async Task HandleOrderCompletedAsync(Order order)
        {
            await _loyalty.AwardCompletedOrderAsync(order);
            await _rewardRules.ApplyOnOrderCompletedAsync(order);
            await _orders.UpdateAsync(order);
        }

        /// <summary>
        /// Post-cancellation side effects: refund redeemed loyalty points;
        /// wallet-paid orders are credited back (item 140).
        /// </summary>
        private async Task HandleOrderCancelledAsync(Order order)
        {
            await _loyalty.RefundRedeemedPointsAsync(order);

            if (order.PaymentMethod == PaymentMethod.Wallet
                && order.PaymentStatus == PaymentStatus.Paid)
            {
                await _wallets.RefundAsync(order.CustomerPhone, order.TotalAmount.Amount, order.OrderNumber);
            }
        }

        /// <summary>
        /// Item 180: posts {maDon, sdt, tongTien, trangThai, thoiGian} to the
        /// shop's configured new-order webhook URL. Fire-and-log with a 5s
        /// timeout — failures only log, never break order creation.
        /// </summary>
        private async Task FireSimpleNewOrderWebhookAsync(Order order)
        {
            try
            {
                var settings = await _shopSettings.GetAsync();
                if (settings == null || !settings.NewOrderWebhookEnabled)
                {
                    return;
                }

                var url = settings.NewOrderWebhookUrl;
                if (string.IsNullOrWhiteSpace(url)
                    || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var sent = await Shop.ShopSettingAppService.PostNewOrderWebhookAsync(
                    _httpClientFactory,
                    url,
                    new
                    {
                        maDon = order.OrderNumber,
                        sdt = order.CustomerPhone,
                        tongTien = order.TotalAmount.Amount,
                        trangThai = order.Status.ToString(),
                        thoiGian = DateTime.UtcNow
                    });

                if (!sent)
                {
                    Logger.LogWarning(
                        "Item 180: new-order webhook to {Url} returned a non-success status for order {OrderNumber}.",
                        url, order.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Item 180: new-order webhook failed for order {OrderNumber}.",
                    order.OrderNumber);
            }
        }

        /// <summary>
        /// Webhook dispatch must never break the order flow: log and continue.
        /// </summary>
        private async Task FireWebhookSafelyAsync(string eventName, object payload)
        {
            try
            {
                await _webhooks.DispatchAsync(eventName, payload);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Webhook dispatch failed for event {Event}.", eventName);
            }
        }

        /// <summary>Item 82: admin assigns (or unassigns) a delivery shipper.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<OrderDto> AssignShipperAsync(Guid id, AssignShipperDto input)
        {
            var order = await FindOrThrowAsync(id);

            if (input.ShipperId.HasValue && input.ShipperId.Value != Guid.Empty)
            {
                var shipper = await _shippers.FindAsync(input.ShipperId.Value);
                if (shipper == null || !shipper.IsActive)
                {
                    throw new Domain.Exceptions.DomainException(
                        "Shipper không tồn tại hoặc đã ngừng hoạt động.");
                }

                order.AssignShipper(shipper.Id);
            }
            else
            {
                order.AssignShipper(null);
            }

            await _orders.UpdateAsync(order);

            var dto = _objectMapper.Map<Order, OrderDto>(order);
            await FillShipperNamesAsync(new[] { dto });
            return dto;
        }

        /// <summary>Item 81: kitchen display — Pending/Confirmed/Preparing, oldest first.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Kitchen")]
        public virtual async Task<List<OrderDto>> GetKitchenOrdersAsync()
        {
            var kitchenStatuses = new[]
                { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Preparing };
            var queryable = await _orders.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.Where(x => kitchenStatuses.Contains(x.Status))
                    .OrderBy(x => x.CreatedAt)
                    .Take(200));
            var dtos = _objectMapper.Map<List<Order>, List<OrderDto>>(items);
            // Wave 6c (item 33): internal note stays in the admin UI.
            dtos.ForEach(x => x.AdminNote = null);
            await FillShipperNamesAsync(dtos);
            await FillCustomerTagsAsync(dtos);
            return dtos;
        }

        /// <summary>
        /// Item 81: kitchen display tap advances one stage:
        /// Pending→Confirmed→Preparing→Delivering.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Kitchen")]
        public virtual async Task<OrderDto> AdvanceKitchenStatusAsync(Guid id)
        {
            var order = await FindOrThrowAsync(id);
            var fromStatus = order.Status;

            switch (order.Status)
            {
                case OrderStatus.Pending:
                    order.Confirm();
                    break;
                case OrderStatus.Confirmed:
                    order.StartPreparing();
                    break;
                case OrderStatus.Preparing:
                    order.StartDelivering();
                    break;
                default:
                    throw new Domain.Exceptions.DomainException(
                        $"Đơn hàng ở trạng thái '{order.Status}' không thể chuyển tiếp từ màn hình bếp.");
            }

            await _orders.UpdateAsync(order);

            // Wave 6c (item 34): audit the KDS tap.
            await RecordStatusChangeAsync(order.Id, fromStatus, order.Status);

            // Item 156: staff activity log (kitchen advances are role-gated,
            // no extra permission needed).
            await _activityLogger.LogAsync(StaffActions.OrderStatusChange,
                $"Bếp chuyển đơn {order.OrderNumber}: {fromStatus} → {order.Status}.");

            var dto = _objectMapper.Map<Order, OrderDto>(order);
            // Wave 6c (item 33): internal note stays in the admin UI.
            dto.AdminNote = null;
            await FillShipperNamesAsync(new[] { dto });
            return dto;
        }

        /// <summary>
        /// Wave 6c (item 34): appends one transition record to the audit
        /// trail. No-op when the status did not change.
        /// </summary>
        private async Task RecordStatusChangeAsync(Guid orderId, OrderStatus from, OrderStatus to)
        {
            if (from == to)
            {
                return;
            }

            var byUser = CurrentUser?.UserName;
            await _statusHistory.InsertAsync(new OrderStatusHistory
            {
                OrderId = orderId,
                FromStatus = from,
                ToStatus = to,
                At = DateTime.UtcNow,
                ByUser = string.IsNullOrWhiteSpace(byUser) ? "hệ thống" : byUser
            }, autoSave: true);
        }

        /// <summary>
        /// Wave 6c (item 33): admin sets or clears the internal note.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<OrderDto> UpdateAdminNoteAsync(Guid id, UpdateAdminNoteDto input)
        {
            var order = await FindOrThrowAsync(id);
            order.SetAdminNote(input?.Note);
            await _orders.UpdateAsync(order);

            var dto = _objectMapper.Map<Order, OrderDto>(order);
            await FillShipperNamesAsync(new[] { dto });
            return dto;
        }

        /// <summary>
        /// Staff confirms a ShipperPickup order was collected at the shop
        /// ("Xác nhận đã lấy hàng"). Optionally records the customer-booked
        /// shipper info. When the order is being prepared, it advances to
        /// Delivering (the shipper is now transporting it).
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<OrderDto> MarkPickedUpAsync(Guid id, MarkPickedUpDto input)
        {
            // Item 155: confirming a pickup needs orders.confirm.
            await _permissions.RequirePermissionAsync(PermissionKeys.OrdersConfirm);

            var order = await FindOrThrowAsync(id);

            order.SetPickupShipperInfo(input?.PickupShipperName, input?.PickupShipperPhone);

            var fromStatus = order.Status;
            order.MarkPickedUp();

            // The shipper now has the food — move Preparing orders forward so
            // the status flow stays truthful. Pending/Confirmed orders keep
            // their status; staff advances them normally.
            if (order.Status == OrderStatus.Preparing)
            {
                order.StartDelivering();
                await RecordStatusChangeAsync(order.Id, fromStatus, order.Status);
            }

            await _orders.UpdateAsync(order);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.OrderPickedUp,
                $"Xác nhận đã lấy hàng đơn {order.OrderNumber}.");

            var dto = _objectMapper.Map<Order, OrderDto>(order);
            await FillShipperNamesAsync(new[] { dto });
            return dto;
        }

        /// <summary>
        /// Item 163: staff confirms cash was collected ("💵 Đã thu tiền").
        /// Typically used on COD orders; the server only requires "not
        /// cancelled, not yet collected".
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<OrderDto> MarkCashCollectedAsync(Guid id)
        {
            // Item 155: cash handling needs orders.confirm-payment.
            await _permissions.RequirePermissionAsync(PermissionKeys.OrdersConfirmPayment);

            var order = await FindOrThrowAsync(id);

            order.MarkCashCollected();
            await _orders.UpdateAsync(order);

            var dto = _objectMapper.Map<Order, OrderDto>(order);
            await FillShipperNamesAsync(new[] { dto });
            return dto;
        }

        /// <summary>
        /// Wave 6c (item 34): status-transition timeline, oldest first.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier,Kitchen")]
        public virtual async Task<List<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id)
        {
            // Throw when the order does not exist (also guards against
            // probing arbitrary ids from the kitchen role).
            await FindOrThrowAsync(id);

            var queryable = await _statusHistory.GetQueryableAsync();
            var items = await _asyncExecuter.ToListAsync(
                queryable.Where(x => x.OrderId == id)
                    .OrderBy(x => x.At));
            return items.Select(x => new OrderStatusHistoryDto
            {
                Id = x.Id,
                OrderId = x.OrderId,
                FromStatus = x.FromStatus.ToString(),
                ToStatus = x.ToStatus.ToString(),
                At = x.At,
                ByUser = x.ByUser
            }).ToList();
        }

        /// <summary>Item 87: admin hides/unhides an order review.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public virtual async Task<OrderReviewDto> SetReviewHiddenAsync(Guid id, SetReviewHiddenDto input)
        {
            var review = await _reviews.GetAsync(id);
            review.IsHidden = input.IsHidden;
            await _reviews.UpdateAsync(review, autoSave: true);
            // Perf: bust the cached public review listing.
            _catalogCache.Invalidate();
            return MapReviewToDto(review);
        }

        /// <summary>Item 87: admin replies to a review (shown publicly).</summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public virtual async Task<OrderReviewDto> ReplyReviewAsync(Guid id, ReplyReviewDto input)
        {
            var review = await _reviews.GetAsync(id);
            review.AdminReply = string.IsNullOrWhiteSpace(input.Reply) ? null : input.Reply.Trim();
            await _reviews.UpdateAsync(review, autoSave: true);
            // Perf: bust the cached public review listing.
            _catalogCache.Invalidate();
            return MapReviewToDto(review);
        }

        /// <summary>Item 52: customer rates the shipper (1-5) for an order review.
        /// Ownership is verified via CustomerPhone — without it anyone could overwrite
        /// anyone else's ShipperRating using review ids from the public listing.</summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public virtual async Task<OrderReviewDto> SetShipperRatingAsync(Guid id, SetShipperRatingDto input)
        {
            if (input.Rating < 1 || input.Rating > 5)
                throw new Domain.Exceptions.DomainException("Đánh giá shipper phải từ 1 đến 5 sao.");

            var phone = NormalizePhoneKey(input.CustomerPhone);
            if (string.IsNullOrEmpty(phone))
                throw new Domain.Exceptions.DomainException("Vui lòng nhập số điện thoại đặt hàng để xác thực.");

            var review = await _reviews.GetAsync(id);
            if (NormalizePhoneKey(review.CustomerPhone) != phone)
                throw new Domain.Exceptions.DomainException("Số điện thoại không khớp với đơn hàng của đánh giá này.");

            review.ShipperRating = input.Rating;
            await _reviews.UpdateAsync(review, autoSave: true);
            return MapReviewToDto(review);
        }

        /// <summary>Item 87: public non-hidden reviews, newest first.
        /// Returns only public-safe fields: phone is masked server-side, and the
        /// review reward voucher code is NEVER exposed (it is bound to the phone).</summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public virtual async Task<List<PublicOrderReviewDto>> GetPublicReviewsAsync()
        {
            // Perf: landing page fetches this on every visit; cache 5 minutes,
            // invalidated on review submit/hide/reply (see Invalidate below).
            return await _catalogCache.GetOrAddAsync("orders:public-reviews", async () =>
            {
                var reviewQueryable = await _reviews.GetQueryableAsync();
                var items = await _asyncExecuter.ToListAsync(
                    reviewQueryable.Where(x => !x.IsHidden)
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(20));
                return items.Select(MapReviewToPublicDto).ToList();
            });
        }

        private static PublicOrderReviewDto MapReviewToPublicDto(OrderReview review)
        {
            return new PublicOrderReviewDto
            {
                MaskedCustomerPhone = MaskPhoneForDisplay(review.CustomerPhone),
                Rating = review.Rating,
                Comment = review.Comment,
                PhotoUrl = review.PhotoUrl,
                AdminReply = review.AdminReply,
                CreatedAt = review.CreatedAt,
                Lines = review.Lines?.Select(x => new OrderReviewLineDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Rating = x.Rating
                }).ToList() ?? new List<OrderReviewLineDto>()
            };
        }

        /// <summary>Server-side phone masking (mirrors landing MaskPhone): "0901234567" → "090***67".</summary>
        private static string MaskPhoneForDisplay(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length < 5) return "Khách hàng";
            return digits.Substring(0, 3) + "***" + digits.Substring(digits.Length - 2);
        }

        public virtual async Task<OrderDto> CancelByCustomerAsync(CancelOrderByCustomerInput input)
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

            // Wave 6c (item 34): audit the customer cancel.
            await RecordStatusChangeAsync(order.Id, OrderStatus.Pending, OrderStatus.Cancelled);

            // Item 140: wallet-paid orders are credited back on cancel.
            if (order.PaymentMethod == PaymentMethod.Wallet
                && order.PaymentStatus == PaymentStatus.Paid)
            {
                await _wallets.RefundAsync(order.CustomerPhone, order.TotalAmount.Amount, order.OrderNumber);
            }

            // Wave 6c (item 33): internal note never leaves on public DTOs.
            var dto = _objectMapper.Map<Order, OrderDto>(order);
            dto.AdminNote = null;
            return dto;
        }

        public virtual async Task<OrderDto> UpdateLinesByCustomerAsync(UpdateOrderLinesInput input)
        {
            var order = await FindByNumberAndPhoneAsync(input.OrderNumber, input.CustomerPhone);

            var oldTotal = order.TotalAmount.Amount;

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

            // Wallet-paid orders: reconcile the wallet with the new total.
            // Without this the wallet stays charged at the old amount while the
            // order total changes (customer could underpay by adding items).
            if (order.PaymentMethod == PaymentMethod.Wallet
                && order.PaymentStatus == PaymentStatus.Paid)
            {
                var delta = order.TotalAmount.Amount - oldTotal;
                if (delta > 0)
                {
                    await _wallets.ChargeAsync(order.CustomerPhone, delta, order.OrderNumber);
                }
                else if (delta < 0)
                {
                    await _wallets.RefundAsync(order.CustomerPhone, -delta, order.OrderNumber);
                }
            }

            await _orders.UpdateAsync(order);
            // Wave 6c (item 33): internal note is never serialized publicly.
            var linesDto = _objectMapper.Map<Order, OrderDto>(order);
            linesDto.AdminNote = null;
            return linesDto;
        }

        public virtual async Task<OrderDto> UpdateAddressByCustomerAsync(UpdateOrderAddressByCustomerInput input)
        {
            var order = await FindByNumberAndPhoneAsync(input.OrderNumber, input.CustomerPhone);

            order.UpdateAddressByCustomer(input.CustomerAddress, input.DeliverySlot);

            await _orders.UpdateAsync(order);
            // Wave 6c (item 33): internal note is never serialized publicly.
            var dto = _objectMapper.Map<Order, OrderDto>(order);
            dto.AdminNote = null;
            return dto;
        }

        public virtual async Task<OrderDto> UpdateContactByCustomerAsync(UpdateOrderContactByCustomerInput input)
        {
            var order = await FindByNumberAndPhoneAsync(input.OrderNumber, input.CustomerPhone);

            order.UpdateContactByCustomer(input.RecipientPhone, input.Notes);

            await _orders.UpdateAsync(order);
            var dto = _objectMapper.Map<Order, OrderDto>(order);
            dto.AdminNote = null;
            return dto;
        }

        public virtual async Task<OrderReviewDto> SubmitReviewAsync(SubmitReviewInput input)
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
                PhotoUrl = string.IsNullOrWhiteSpace(input.PhotoUrl) ? null : input.PhotoUrl.Trim(),
                CreatedAt = DateTime.UtcNow,
                // Wave 6b (item 26): per-dish ratings. Only products that
                // belong to this order are kept; one rating per product.
                Lines = await BuildReviewLinesAsync(order.Id, input.Lines)
            };
            await _reviews.InsertAsync(review, autoSave: true);

            // Review kèm ảnh được tặng 1 phần xôi: tạo voucher giảm 25.000đ
            // gắn với SĐT khách, dùng cho đơn sau (hết hạn sau 30 ngày).
            if (!string.IsNullOrWhiteSpace(review.PhotoUrl))
            {
                var phone = _loyalty.NormalizePhone(order.CustomerPhone);
                var voucher = await _discounts.CreateGeneratedAsync(
                    new Discounts.DTOs.CreateGeneratedDiscountDto
                    {
                        Prefix = "REVIEW",
                        DiscountAmount = 25000,
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        OwnerPhone = phone
                    });
                review.RewardCode = voucher.Code;
                await _reviews.UpdateAsync(review, autoSave: true);
            }

            // Item 134: đánh giá 5 sao được thưởng +20 điểm loyalty.
            if (review.Rating >= 5)
            {
                try
                {
                    await _loyalty.AdjustAsync(order.CustomerPhone, new AdjustPointsInput
                    {
                        PointsDelta = 20,
                        Reason = $"Thưởng đánh giá 5 sao đơn {order.OrderNumber}"
                    });
                }
                catch (Exception)
                {
                    // Thưởng điểm là nice-to-have — không fail review vì lỗi này.
                }
            }

            // Perf: bust the cached public review listing.
            _catalogCache.Invalidate();

            return MapReviewToDto(review);
        }

        /// <summary>
        /// Wave 6b (item 26): validates per-dish ratings against the order's
        /// own lines and resolves the product names server-side.
        /// </summary>
        private async Task<List<OrderReviewLine>> BuildReviewLinesAsync(
            Guid orderId, List<SubmitReviewLineInput> lines)
        {
            var result = new List<OrderReviewLine>();
            if (lines == null || lines.Count == 0)
            {
                return result;
            }

            var orderQueryable = await _orders.GetQueryableAsync();
            // One order's lines only — grouped client-side to avoid
            // provider-specific GROUP BY translation issues.
            var orderLines = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Id == orderId)
                    .SelectMany(x => x.Lines)
                    .Select(x => new { x.ProductId, x.ProductName }));
            var productNames = orderLines
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.First().ProductName);

            foreach (var line in lines)
            {
                if (line == null || line.Rating < 1 || line.Rating > 5)
                {
                    continue;
                }

                var product = productNames.TryGetValue(line.ProductId, out var productName)
                    ? productName
                    : null;
                if (product == null)
                {
                    continue;
                }

                if (result.Any(x => x.ProductId == line.ProductId))
                {
                    continue;
                }

                result.Add(new OrderReviewLine
                {
                    ProductId = line.ProductId,
                    ProductName = product,
                    Rating = line.Rating
                });
            }

            return result;
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public virtual async Task<List<OrderReviewDto>> GetReviewsAsync()
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
        public virtual async Task<SpinResultDto> SpinAsync(SpinInput input)
        {
            // Item 105: feature-flagged. The frontend hides the wheel, but
            // the backend is the real gate.
            var flags = LazyServiceProvider.LazyGetRequiredService<IFeatureFlagAppService>();
            if (!await flags.IsEnabledAsync(FeatureFlag.LuckySpin))
            {
                throw new Domain.Exceptions.DomainException("Vòng quay may mắn hiện đang tắt.");
            }

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
            var expiresAt = DateTime.UtcNow.AddDays(SpinCodeValidityDays);

            string code = null;
            string prizeLabel;
            switch (segment)
            {
                case "Percent5":
                    prizeLabel = "Giảm 5%";
                    code = await CreateSpinPrizeCodeAsync(SpinCodePrefix, expiresAt, phone, discountPercent: 5);
                    break;
                case "Percent10":
                    prizeLabel = "Giảm 10%";
                    code = await CreateSpinPrizeCodeAsync(SpinCodePrefix, expiresAt, phone, discountPercent: 10);
                    break;
                case "Percent15":
                    prizeLabel = "Giảm 15%";
                    code = await CreateSpinPrizeCodeAsync(SpinCodePrefix, expiresAt, phone, discountPercent: 15);
                    break;
                case "FreeShip":
                    prizeLabel = "Miễn phí ship";
                    var freeshipAmount = settings.DeliveryFee > 0 ? settings.DeliveryFee : SpinFreeshipFallbackAmount;
                    code = await CreateSpinPrizeCodeAsync(SpinFreeshipCodePrefix, expiresAt, phone, discountAmount: freeshipAmount);
                    break;
                case "Cash20k":
                    prizeLabel = "Giảm 20.000đ";
                    code = await CreateSpinPrizeCodeAsync(SpinCodePrefix, expiresAt, phone, discountAmount: 20000);
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
        /// Generates a single-use discount code for a lucky-spin prize.
        /// </summary>
        private async Task<string> CreateSpinPrizeCodeAsync(
            string prefix, DateTime expiresAt, string ownerPhone,
            decimal? discountPercent = null, decimal? discountAmount = null)
        {
            var result = await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
            {
                Prefix = prefix,
                DiscountPercent = discountPercent,
                DiscountAmount = discountAmount,
                ExpiresAt = expiresAt,
                OwnerPhone = ownerPhone
            });
            return result.Code;
        }

        /// <summary>
        /// Cancels Pending bank-transfer orders that stayed unpaid past the
        /// configured expiry (worker entry point).
        /// </summary>
        public virtual async Task<int> AutoCancelExpiredBankTransfersAsync()
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
                // Wave 6c (item 34): audit the worker cancel ("hệ thống").
                await RecordStatusChangeAsync(order.Id, OrderStatus.Pending, OrderStatus.Cancelled);
                await _loyalty.RefundRedeemedPointsAsync(order);
            }

            return expired.Count;
        }

        /// <summary>
        /// Public lookup that also proves ownership via phone match.
        /// </summary>
        /// <summary>
        /// Wave 6b (item 30): rejects checkout when the pause toggle is off
        /// or the current Vietnam time (UTC+7, no DST) is outside the
        /// configured open hours. Strict: no grace period after CloseTime.
        /// </summary>
        private static void EnsureAcceptingOrders(ShopSettingDto shopSetting)
        {
            // Item 176: maintenance mode blocks order creation on every
            // channel (web, POS, group orders, Messenger — all go through
            // CreateAsync). The public WASM app shows the maintenance page
            // instead; this is the server-side backstop.
            if (shopSetting.MaintenanceMode)
            {
                var message = (shopSetting.MaintenanceMessage ?? string.Empty).Trim();
                throw new Domain.Exceptions.DomainException(
                    string.IsNullOrEmpty(message)
                        ? "Quán đang bảo trì. Vui lòng quay lại sau."
                        : message);
            }

            if (!shopSetting.AcceptingOrders)
            {
                var reason = (shopSetting.PausedReason ?? string.Empty).Trim();
                // Item 117: on a holiday the banner reason is the whole message.
                throw new Domain.Exceptions.DomainException(
                    shopSetting.IsHolidayPaused && reason.Length > 0
                        ? reason
                        : string.IsNullOrEmpty(reason)
                            ? "Quán đang tạm ngừng nhận đơn. Vui lòng quay lại sau."
                            : $"Quán đang tạm ngừng nhận đơn: {reason}");
            }

            var now = DateTimeOffset.UtcNow.AddHours(7).TimeOfDay;
            var open = ParseHour(shopSetting.OpenTime, "06:00");
            var close = ParseHour(shopSetting.CloseTime, "10:30");
            if (close > open && (now < open || now >= close))
            {
                throw new Domain.Exceptions.DomainException(
                    $"Quán chỉ nhận đơn từ {FormatHour(open)} đến {FormatHour(close)}. " +
                    "Vui lòng quay lại trong giờ mở cửa.");
            }
        }

        private static TimeSpan ParseHour(string value, string fallback)
        {
            if (TimeSpan.TryParse(value?.Trim(), out var time)
                && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1))
            {
                return time;
            }

            TimeSpan.TryParse(fallback, out var fallbackTime);
            return fallbackTime;
        }

        private static string FormatHour(TimeSpan time)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}";
        }

        private async Task<Order> FindByNumberAndPhoneAsync(string orderNumber, string customerPhone)        {
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
                PhotoUrl = review.PhotoUrl,
                RewardCode = review.RewardCode,
                IsHidden = review.IsHidden,
                AdminReply = review.AdminReply,
                CreatedAt = review.CreatedAt,
                Lines = review.Lines?.Select(x => new OrderReviewLineDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Rating = x.Rating
                }).ToList() ?? new List<OrderReviewLineDto>()
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
            var roll = Random.Shared.Next(total);
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

        public virtual async Task<OrderDto> MarkTransferredAsync(Guid id, MarkTransferredDto input)
        {
            var order = await FindOrThrowAsync(id);

            if (!PhonesMatch(order.CustomerPhone, input.CustomerPhone))
            {
                throw new Domain.Exceptions.DomainException("Số điện thoại không khớp với đơn hàng.");
            }

            order.MarkTransferred();
            await _orders.UpdateAsync(order);

            // Wave 6c (item 33): internal note is never serialized publicly.
            var transferredDto = _objectMapper.Map<Order, OrderDto>(order);
            transferredDto.AdminNote = null;
            return transferredDto;
        }

        public virtual async Task<OrderDto> ConfirmPaymentAsync(Guid id)
        {
            // Item 155: confirming "money received" needs orders.confirm-payment.
            await _permissions.RequirePermissionAsync(PermissionKeys.OrdersConfirmPayment);

            var order = await FindOrThrowAsync(id);
            order.ConfirmPayment();
            await _orders.UpdateAsync(order);

            // Item 156: staff activity log.
            await _activityLogger.LogAsync(StaffActions.OrderConfirmPayment,
                $"Xác nhận đã nhận tiền đơn {order.OrderNumber}.");

            return _objectMapper.Map<Order, OrderDto>(order);
        }

        public virtual async Task<MoMoCreatePaymentResultDto> CreateMoMoPaymentAsync(Guid id)
        {
            var order = await FindOrThrowAsync(id);
            return await _momo.CreatePaymentAsync(order);
        }

        public virtual async Task<OrderDto> TrackOrderAsync(TrackOrderInput input)
        {
            var order = await _orders.GetByOrderNumberAsync(input.OrderNumber?.Trim());
            if (order == null || !PhonesMatch(order.CustomerPhone, input.CustomerPhone))
            {
                // Same error for "not found" and "phone mismatch" so the
                // endpoint cannot be used to probe order numbers.
                throw new OrderNotFoundException("Order not found. Please check the order number and phone.");
            }

            // Wave 6c (item 33): internal note is never serialized publicly.
            var dto = _objectMapper.Map<Order, OrderDto>(order);
            dto.AdminNote = null;
            return dto;
        }

        /// <summary>
        /// Admin: customers aggregated by phone number (order count, total
        /// spent, last order). Cancelled orders are excluded from the totals.
        /// </summary>
        public virtual async Task<List<CustomerDto>> GetCustomersAsync(
            string searchTerm, int skip = 0, int take = 50)
        {
            var orderQueryable = await _orders.GetQueryableAsync();
            var query = orderQueryable.Where(x => x.Status != OrderStatus.Cancelled);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(x =>
                    x.CustomerPhone.Contains(term) || x.CustomerName.Contains(term));
            }

            var customers = await _asyncExecuter.ToListAsync(
                query.GroupBy(x => x.CustomerPhone)
                    .Select(g => new CustomerDto
                    {
                        CustomerPhone = g.Key,
                        CustomerName = g.Max(x => x.CustomerName),
                        OrderCount = g.LongCount(),
                        TotalSpent = g.Sum(x => x.TotalAmount.Amount),
                        LastOrderAt = g.Max(x => x.CreatedAt)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .Skip(Math.Max(0, skip))
                    .Take(Math.Clamp(take, 1, 200)));

            // Item 149: attach tags (single lookup, no N+1).
            var tagsByPhone = await _customerTags.GetTagsByPhonesAsync(
                customers.Select(x => x.CustomerPhone));
            foreach (var c in customers)
            {
                if (tagsByPhone.TryGetValue(c.CustomerPhone, out var tags))
                {
                    c.Tags = tags;
                    // Item 130 (đúng): VIP flag drives the one-tap VIP toggle.
                    c.IsVip = tags.Any(t =>
                        string.Equals(t.TagName, "VIP", StringComparison.OrdinalIgnoreCase));
                }
            }

            return customers;
        }

        public virtual async Task<List<CustomerDto>> GetInactiveCustomersAsync(int days, int take = 50)
        {
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, days));
            var orderQueryable = await _orders.GetQueryableAsync();

            var customers = await _asyncExecuter.ToListAsync(
                orderQueryable.Where(x => x.Status != OrderStatus.Cancelled)
                    .GroupBy(x => x.CustomerPhone)
                    .Select(g => new CustomerDto
                    {
                        CustomerPhone = g.Key,
                        CustomerName = g.Max(x => x.CustomerName),
                        OrderCount = g.LongCount(),
                        TotalSpent = g.Sum(x => x.TotalAmount.Amount),
                        LastOrderAt = g.Max(x => x.CreatedAt)
                    })
                    .Where(x => x.LastOrderAt < cutoff)
                    .OrderBy(x => x.LastOrderAt)
                    .Take(Math.Clamp(take, 1, 200)));

            return customers;
        }
        /// same phone (tolerant match), placed within the last 30 minutes,
        /// still Pending or Confirmed. The shop never merges automatically;
        /// this list only feeds the admin review panel.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public virtual async Task<List<DuplicateOrderGroupDto>> GetDuplicateOrderGroupsAsync()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            var orderQueryable = await _orders.GetQueryableAsync();
            var candidates = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => (x.Status == OrderStatus.Pending || x.Status == OrderStatus.Confirmed)
                        && x.CreatedAt >= cutoff)
                    .OrderBy(x => x.CreatedAt));

            var groups = candidates
                .Where(x => !string.IsNullOrWhiteSpace(x.CustomerPhone))
                .GroupBy(x => NormalizePhoneKey(x.CustomerPhone))
                .Where(g => g.Key != null && g.Count() > 1)
                .Select(g =>
                {
                    var dtos = g.Select(o => _objectMapper.Map<Order, OrderDto>(o)).ToList();
                    return new DuplicateOrderGroupDto
                    {
                        Phone = g.First().CustomerPhone,
                        CustomerName = g.First().CustomerName,
                        Orders = dtos
                    };
                })
                .ToList();

            await FillShipperNamesAsync(groups.SelectMany(g => g.Orders).ToList());
            return groups;
        }

        /// <summary>
        /// Item 142 (Admin/Cashier): merges the selected orders' lines into
        /// the primary order, cancels the merged orders with an admin note
        /// ("gộp vào đơn ..."). Totals are recalculated server-side.
        /// Always explicit — the shop must press the button.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Cashier")]
        public virtual async Task<OrderDto> MergeOrdersAsync(MergeOrdersInput input)
        {
            if (input == null || input.OrderIds == null || input.OrderIds.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Chưa chọn đơn nào để gộp.");
            }

            var primary = await FindOrThrowAsync(input.PrimaryOrderId);
            EnsureMergeable(primary);

            // P9: batch load all secondaries in 1 query instead of N FindOrThrowAsync calls.
            var secondaryIds = input.OrderIds.Distinct()
                .Where(x => x != primary.Id).ToList();
            var withDetails = await _orders.WithDetailsAsync(x => x.Lines);
            var secondaryDict = (await _asyncExecuter.ToListAsync(
                    withDetails.Where(x => secondaryIds.Contains(x.Id))))
                .ToDictionary(x => x.Id);

            var secondaries = new List<Order>();
            foreach (var id in secondaryIds)
            {
                if (!secondaryDict.TryGetValue(id, out var other))
                {
                    throw new OrderNotFoundException(id);
                }

                EnsureMergeable(other);
                secondaries.Add(other);
            }

            if (secondaries.Count == 0)
            {
                throw new Domain.Exceptions.DomainException("Không có đơn hợp lệ để gộp.");
            }

            foreach (var other in secondaries)
            {
                primary.MergeLinesFrom(other);

                var stamp = $"Gộp vào đơn {primary.OrderNumber} lúc {DateTime.UtcNow:dd/MM HH:mm}.";
                primary.SetAdminNote(AppendNote(primary.AdminNote, stamp));

                var existing = other.AdminNote;
                other.SetAdminNote(AppendNote(existing, $"Gộp vào đơn {primary.OrderNumber}."));

                var fromStatus = other.Status;
                other.Cancel();
                await _orders.UpdateAsync(other);
                await RecordStatusChangeAsync(other.Id, fromStatus, OrderStatus.Cancelled);

                // Mirror the cancel path: refund redeemed loyalty points and
                // wallet payments so the customer does not lose money.
                await _loyalty.RefundRedeemedPointsAsync(other);
                if (other.PaymentMethod == PaymentMethod.Wallet
                    && other.PaymentStatus == PaymentStatus.Paid)
                {
                    await _wallets.RefundAsync(other.CustomerPhone, other.TotalAmount.Amount, other.OrderNumber);
                }
            }

            await _orders.UpdateAsync(primary);

            var dto = _objectMapper.Map<Order, OrderDto>(primary);
            await FillShipperNamesAsync(new[] { dto });
            return dto;
        }

        private static void EnsureMergeable(Order order)
        {
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Đơn {order.OrderNumber} không còn ở trạng thái có thể gộp (chờ xác nhận/đã xác nhận).");
            }
        }

        private static string NormalizePhoneKey(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length >= 9 ? digits.Substring(digits.Length - 9) : null;
        }

        private static string AppendNote(string existing, string addition)
        {
            if (string.IsNullOrWhiteSpace(existing))
            {
                return addition;
            }

            return existing.Trim() + " | " + addition;
        }

        /// <summary>
        /// Public: last order info for a phone number — only the customer
        /// name and delivery address (no order details, no sensitive data),
        /// so the checkout form can offer one-click address re-fill.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public virtual async Task<LastOrderInfoDto> GetLastOrderInfoByPhoneAsync(string phone)
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

        /// <summary>Item 143: true when the phone placed at least one order.
        /// Compares normalized phones (digits, last-9 tolerant) so customers
        /// cannot dodge the check by reformatting their own number.</summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public virtual async Task<bool> HasOrderedBeforeAsync(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length < 9)
            {
                return false;
            }

            var suffix = digits.Substring(digits.Length - 9);
            var orderQueryable = await _orders.GetQueryableAsync();
            // SQL pre-filter on the phone suffix (LIKE, provider-agnostic), then the
            // exact tolerant match in memory — same pattern as GetLastOrderInfoByPhoneAsync.
            var candidates = await _asyncExecuter.ToListAsync(
                orderQueryable.Where(x => x.CustomerPhone != null && x.CustomerPhone.Contains(suffix)));
            return candidates.Any(x => PhonesMatch(x.CustomerPhone, phone));
        }

        /// <summary>
        /// Item 143: first-order voucher for a new customer. Throws
        /// DomainException on an invalid phone.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public virtual async Task<FirstOrderVoucherDto> GetFirstOrderVoucherAsync(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length < 9)
            {
                throw new Domain.Exceptions.DomainException(
                    "Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            // Normalize once (digits-only): the history check, the existing-code
            // lookup and the generated code's OwnerPhone must all use the same
            // form, otherwise reformatting the same number farms unlimited vouchers.
            if (await HasOrderedBeforeAsync(digits))
            {
                return new FirstOrderVoucherDto { IsNewCustomer = false };
            }

            var existing = await _discounts.GetUsableCodeAsync("CHAO", digits);
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return new FirstOrderVoucherDto
                {
                    IsNewCustomer = true,
                    Code = existing,
                    DiscountPercent = 10
                };
            }

            var created = await _discounts.CreateGeneratedAsync(new CreateGeneratedDiscountDto
            {
                Prefix = "CHAO",
                DiscountPercent = 10,
                MinOrderAmount = 0,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                OwnerPhone = digits
            });

            return new FirstOrderVoucherDto
            {
                IsNewCustomer = true,
                Code = created.Code,
                DiscountPercent = 10
            };
        }

        /// <summary>
        /// Public: full order history for a VN phone number (no login).
        /// Strict VN mobile format required. Only orders whose phone matches
        /// are returned; AdminNote is stripped like other public endpoints.
        /// Returns an empty list when the phone has no orders.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public virtual async Task<List<OrderDto>> GetOrdersByPhoneAsync(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (!System.Text.RegularExpressions.Regex.IsMatch(digits, @"^(0)(3|5|7|8|9)\d{8}$"))
            {
                throw new CrossCuttingConcerns.Exceptions.ValidationException(
                    "Số điện thoại không đúng định dạng (VD: 0901234567).");
            }

            var last9 = digits.Substring(digits.Length - 9);
            var orderQueryable = await _orders.GetQueryableAsync();
            var candidates = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.CustomerPhone.Contains(last9))
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(100));

            var result = new List<OrderDto>();
            foreach (var order in candidates.Where(x => PhonesMatch(x.CustomerPhone, digits)))
            {
                var dto = _objectMapper.Map<Order, OrderDto>(order);
                dto.AdminNote = null;
                result.Add(dto);
            }

            return result;
        }

        private async Task<Order> FindOrThrowAsync(Guid id)
        {
            // P6: WithDetailsAsync loads Lines explicitly,
            // replacing the removed global AutoInclude.
            var queryable = await _orders.WithDetailsAsync(x => x.Lines);
            var order = await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.Id == id));
            if (order == null)
            {
                throw new OrderNotFoundException(id);
            }

            return order;
        }

        /// <summary>
        /// Item 97: finds an order created with the same idempotency key
        /// within the last 24h (a retry of the same checkout attempt).
        /// </summary>
        private async Task<Order> FindByIdempotencyKeyAsync(string idempotencyKey)
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var queryable = await _orders.GetQueryableAsync();
            return await _asyncExecuter.FirstOrDefaultAsync(
                queryable.Where(x => x.IdempotencyKey == idempotencyKey && x.CreatedAt >= cutoff));
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var random = Random.Shared;

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
        /// <summary>
        /// Wave 6d (item 39): counts today's non-cancelled orders for the
        /// phone (Vietnam day) and rejects when the per-day cap is reached.
        /// 0 = unlimited. Phone comparison is tolerant (last 9 digits).
        /// perf-hunt 2026-10-19: the count now runs in SQL (COUNT with the
        /// separators stripped via REPLACE + a suffix LIKE on the last 9
        /// digits) instead of loading every phone of the day into memory on
        /// each checkout. Equivalent to PhonesMatch for realistic inputs
        /// (digits + spaces/dashes/dots + leading country code); EF Core
        /// translates the chained Replace/EndsWith to nested REPLACE + LIKE.
        /// </summary>
        private async Task EnsurePhoneDailyLimitAsync(int maxPerDay, string customerPhone)
        {
            if (maxPerDay <= 0 || string.IsNullOrWhiteSpace(customerPhone))
            {
                return;
            }

            var startOfTodayUtc = VietnamTime.Today().AddHours(-7); // VN 00:00 → UTC

            var orderQueryable = await _orders.GetQueryableAsync();
            var todayQuery = orderQueryable
                .Where(x => x.Status != OrderStatus.Cancelled
                    && x.CreatedAt >= startOfTodayUtc);

            // Same digit-tolerance as PhonesMatch: compare the last 9 digits
            // (or the exact digit string when shorter than 9 digits).
            var digits = new string(customerPhone.Where(char.IsDigit).ToArray());
            int count;
            if (digits.Length >= 9)
            {
                var last9 = digits.Substring(digits.Length - 9);
                count = await _asyncExecuter.CountAsync(todayQuery.Where(x =>
                    x.CustomerPhone.Replace(" ", "").Replace("-", "").Replace(".", "").EndsWith(last9)));
            }
            else
            {
                count = await _asyncExecuter.CountAsync(todayQuery.Where(x =>
                    x.CustomerPhone.Replace(" ", "").Replace("-", "").Replace(".", "") == digits));
            }

            if (count >= maxPerDay)
            {
                throw new Domain.Exceptions.DomainException(
                    $"Số điện thoại này đã đặt tối đa {maxPerDay} đơn hôm nay. Vui lòng liên hệ quán để đặt thêm.");
            }
        }

        /// <summary>
        /// Item 165: rejects lines whose product would exceed its daily
        /// portion limit. Combos are excluded (they are not portion-based).
        /// Best-effort against races — see the call site comment.
        /// </summary>
        private async Task EnsureDailyLimitsAsync(
            List<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity,
                string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo, string Note)> lines)
        {
            var wanted = lines
                .Where(x => !x.IsCombo)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => new
                {
                    Name = g.First().ProductName,
                    Quantity = g.Sum(x => x.Quantity)
                });

            if (wanted.Count == 0)
            {
                return;
            }

            var productQueryable = await _products.GetQueryableAsync();
            var limits = await _asyncExecuter.ToListAsync(
                productQueryable
                    .Where(x => wanted.Keys.Contains(x.Id) && x.DailyLimit != null)
                    .Select(x => new { x.Id, Limit = x.DailyLimit.Value }));

            if (limits.Count == 0)
            {
                return;
            }

            var today = VietnamTime.Today();
            var fromUtc = VietnamTime.DayStartUtc(today);
            var toUtc = VietnamTime.DayEndExclusiveUtc(today);

            var orderQueryable = await _orders.GetQueryableAsync();
            var soldRows = await _asyncExecuter.ToListAsync(
                orderQueryable
                    .Where(x => x.Status != OrderStatus.Cancelled
                        && x.CreatedAt >= fromUtc
                        && x.CreatedAt < toUtc)
                    .SelectMany(x => x.Lines)
                    .GroupBy(l => l.ProductId)
                    .Select(g => new { ProductId = g.Key, Sold = g.Sum(l => l.Quantity) }));

            var soldMap = soldRows.ToDictionary(x => x.ProductId, x => (int)x.Sold);

            foreach (var limited in limits)
            {
                var want = wanted[limited.Id];
                var sold = soldMap.TryGetValue(limited.Id, out var s) ? s : 0;
                if (sold + want.Quantity > limited.Limit)
                {
                    var remaining = Math.Max(0, limited.Limit - sold);
                    throw new Domain.Exceptions.DomainException(
                        $"Món '{want.Name}' hôm nay chỉ còn {remaining} suất. Vui lòng giảm số lượng.");
                }
            }
        }

        /// <summary>
        /// Item 164: the instant used for sale-window checks — the scheduled
        /// pickup time for pre-orders, now for immediate orders.
        /// </summary>
        private static DateTime GetAvailabilityCheckTime(CreateOrderDto input)
        {
            if (input.ScheduledFor.HasValue)
            {
                var sf = input.ScheduledFor.Value;
                if (sf.Kind == DateTimeKind.Unspecified)
                {
                    sf = DateTime.SpecifyKind(sf, DateTimeKind.Local).ToUniversalTime();
                }
                else if (sf.Kind == DateTimeKind.Local)
                {
                    sf = sf.ToUniversalTime();
                }

                return sf;
            }

            return DateTime.UtcNow;
        }

        internal static bool PhonesMatch(string orderPhone, string givenPhone)        {
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
