using PhuThuongStickyRice.Domain.Exceptions;
using PhuThuongStickyRice.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Customer order aggregate root. All state changes go through
    /// intention-revealing behaviors so domain invariants are enforced in one place.
    /// </summary>
    public class Order : Entity<Guid>, IAggregateRoot
    {
        public const int OrderNumberMaxLength = 30;
        public const int CustomerNameMaxLength = 200;
        public const int CustomerPhoneMaxLength = 30;
        /// <summary>
        /// Độ dài tối đa của <see cref="CustomerPhoneNorm"/> — 9 số cuối
        /// của SĐT (chỉ chữ số). SĐT ngắn hơn 9 số được giữ nguyên.
        /// </summary>
        public const int CustomerPhoneNormMaxLength = 9;
        public const int CustomerEmailMaxLength = 254;
        public const int CustomerAddressMaxLength = 500;
        public const int NotesMaxLength = 1000;
        public const int DiscountCodeMaxLength = 50;
        public const int TaxCodeMaxLength = 30;
        public const int CompanyNameMaxLength = 300;
        public const int RecipientNameMaxLength = 200;
        public const int RecipientPhoneMaxLength = 30;
        public const int GiftMessageMaxLength = 1000;
        public const int ReferralCodeMaxLength = 20;
        public const int IdempotencyKeyMaxLength = 100;
        public const int PromotionNameMaxLength = 100;
        public const int SourceMaxLength = 50;
        public const int DeliverySlotMaxLength = 50;

        /// <summary>
        /// Wave 6c (item 33): internal note for staff only. Never exposed
        /// on public (track-order / customer) endpoints.
        /// </summary>
        public const int AdminNoteMaxLength = 2000;

        /// <summary>Customer-booked pickup shipper name (optional).</summary>
        public const int PickupShipperNameMaxLength = 200;

        /// <summary>Customer-booked pickup shipper phone (optional).</summary>
        public const int PickupShipperPhoneMaxLength = 30;

        /// <summary>Item 233: độ dài tối đa của lý do hủy đơn.</summary>
        public const int CancelReasonMaxLength = 100;

        /// <summary>Mã đơn hiển thị cho khách, vd "PX-20261019-4821" (duy nhất).</summary>
        public string OrderNumber { get; private set; }

        /// <summary>Tên khách đặt (người thanh toán).</summary>
        public string CustomerName { get; private set; }

        /// <summary>SĐT khách đặt — khóa tra cứu đơn, loyalty và voucher gắn SĐT.</summary>
        public string CustomerPhone { get; private set; }

        /// <summary>
        /// SĐT chuẩn hóa phục vụ tra cứu khoan dung định dạng: chỉ giữ chữ
        /// số, lấy 9 số cuối (vd "0912 345 678", "+84912345678" và
        /// "0912345678" đều → "912345678"). Được tính 1 lần trong
        /// constructor — <see cref="CustomerPhone"/> không bao giờ đổi sau
        /// khi tạo đơn — nên mọi đơn mới đều có giá trị đúng. Các tra cứu
        /// theo SĐT (HasOrderedBefore, giới hạn đơn/ngày, voucher đơn đầu,
        /// bonus giới thiệu) so sánh bằng trên cột này để dùng index thay
        /// vì LIKE '%...%' quét toàn bảng; bước kiểm tra khoan dung cuối
        /// cùng trên C# vẫn giữ nguyên nên ngữ nghĩa không đổi.
        /// </summary>
        public string CustomerPhoneNorm { get; private set; }

        /// <summary>
        /// Optional. When provided, the order confirmation email is sent here.
        /// Customers without an email simply do not get an email.
        /// </summary>
        public string CustomerEmail { get; private set; }

        /// <summary>Địa chỉ giao hàng (null với đơn tự đến lấy).</summary>
        public string CustomerAddress { get; private set; }

        /// <summary>Ghi chú của khách cho cả đơn (vd giờ lấy, dị ứng).</summary>
        public string Notes { get; private set; }

        /// <summary>
        /// Trạng thái xử lý đơn (Pending → Confirmed → Preparing → Delivering →
        /// Completed/Cancelled) — khác <see cref="PaymentStatus"/> là trạng thái tiền.
        /// </summary>
        public OrderStatus Status { get; private set; }

        /// <summary>Hình thức thanh toán khách chọn lúc checkout (tiền mặt/chuyển khoản/MoMo).</summary>
        public PaymentMethod PaymentMethod { get; private set; }

        /// <summary>
        /// Trạng thái thanh toán (chưa thu / đã thu / cần đặt cọc) — khác
        /// <see cref="Status"/> là trạng thái xử lý đơn.
        /// </summary>
        public PaymentStatus PaymentStatus { get; private set; }

        /// <summary>Tổng tiền khách phải trả (sau mọi khoản giảm + phí), tính bằng VND.</summary>
        public Money TotalAmount { get; private set; }

        /// <summary>Delivery or self-pickup.</summary>
        public FulfillmentMethod FulfillmentMethod { get; private set; }

        /// <summary>Delivery fee charged on this order (0 when free shipping or pickup).</summary>
        public Money DeliveryFee { get; private set; }

        /// <summary>Gift packaging fee (0 when not a gift).</summary>
        public Money GiftFee { get; private set; }

        /// <summary>Discount code applied by the customer, if any.</summary>
        public string DiscountCode { get; private set; }

        /// <summary>Discount amount subtracted from the subtotal (0 when none).</summary>
        public Money DiscountAmount { get; private set; }

        /// <summary>Loyalty-points discount in VND (0 when none).</summary>
        public Money LoyaltyDiscount { get; private set; }

        /// <summary>Points redeemed for this order.</summary>
        public int LoyaltyPointsRedeemed { get; private set; }

        /// <summary>Membership-tier discount in VND (0 when none).</summary>
        public Money TierDiscount { get; private set; }

        /// <summary>Referrer's referral code used at checkout, if any.</summary>
        public string ReferralCodeUsed { get; private set; }

        /// <summary>Optional VAT invoice tax code.</summary>
        public string TaxCode { get; private set; }

        /// <summary>Optional VAT invoice company name.</summary>
        public string CompanyName { get; private set; }

        /// <summary>True when the order total reached the deposit threshold on bank transfer.</summary>
        public bool RequiresDeposit { get; private set; }

        /// <summary>Recipient (when ordering for someone else).</summary>
        public string RecipientName { get; private set; }

        /// <summary>SĐT người nhận hộ (khi đặt cho người khác; null = khách tự nhận).</summary>
        public string RecipientPhone { get; private set; }

        /// <summary>Gift packaging requested.</summary>
        public bool IsGift { get; private set; }

        /// <summary>Lời nhắn in kèm khi gói quà (null khi không phải quà tặng).</summary>
        public string GiftMessage { get; private set; }

        /// <summary>Customer already used the post-checkout lucky spin for this order.</summary>
        public bool LuckySpinUsed { get; private set; }

        /// <summary>Assigned delivery shipper (nullable). Set by the admin.</summary>
        public Guid? ShipperId { get; private set; }

        /// <summary>
        /// Registered customer account that placed the order (nullable).
        /// Set server-side from the authenticated user at creation.
        /// </summary>
        public Guid? CustomerUserId { get; private set; }

        /// <summary>Navigation tới shipper được gán (null khi chưa gán).</summary>
        public Shipper Shipper { get; private set; }

        /// <summary>
        /// Branch the order belongs to (nullable, item 109). Shown on the
        /// admin order list/print.
        /// </summary>
        public Guid? BranchId { get; private set; }

        /// <summary>Branch display name (denormalized at creation).</summary>
        public string BranchName { get; private set; }

        /// <summary>Assigns or clears the branch (admin).</summary>
        public void ChangeBranch(Guid? branchId, string branchName)
        {
            BranchId = branchId;
            BranchName = string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim();
        }

        /// <summary>
        /// Idempotency key from the checkout attempt. A retry with the same
        /// key within 24h returns the existing order instead of creating a new one.
        /// </summary>
        public string IdempotencyKey { get; private set; }

        /// <summary>Auto-applied "giờ vàng" promotion name, if any.</summary>
        public string PromotionName { get; private set; }

        /// <summary>Auto-applied promotion discount in the order currency.</summary>
        public decimal PromotionDiscountAmount { get; private set; }

        /// <summary>
        /// Where the order came from (item 133): "Web", "QR bàn",
        /// "Link chia sẻ" or "Nhóm". Set at creation from shop entry params.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Wave 6a (item 23): scheduled delivery slot chosen at checkout,
        /// e.g. "06:00–06:30". Null = giao ngay (deliver ASAP).
        /// </summary>
        public string DeliverySlot { get; private set; }

        /// <summary>
        /// Item 128: true when a MoMo IPN callback was matched to this order
        /// by parsing the order number out of the callback's OrderInfo.
        /// </summary>
        public bool MoMoMatched { get; private set; }

        /// <summary>Refund state (item 130): money movement is manual.</summary>
        public RefundStatus RefundStatus { get; private set; }

        /// <summary>Refund amount in VND (0 when no refund).</summary>
        public decimal RefundAmount { get; private set; }

        /// <summary>
        /// Internal staff note (Wave 6c, item 33). Nullable; never exposed
        /// on public endpoints.
        /// </summary>
        public string AdminNote { get; private set; }

        /// <summary>
        /// Name of the customer-booked shipper (Grab/Ahamove/...) who will
        /// pick up a ShipperPickup order. Optional — the customer may fill
        /// it in at checkout or the staff may record it later.
        /// Distinct from <see cref="Shipper"/> (the shop's own delivery staff).
        /// </summary>
        public string PickupShipperName { get; private set; }

        /// <summary>Phone of the customer-booked pickup shipper. Optional.</summary>
        public string PickupShipperPhone { get; private set; }

        /// <summary>
        /// When the shipper/customer collected a ShipperPickup order at the
        /// shop (UTC). Set by staff via "Xác nhận đã lấy hàng".
        /// </summary>
        public DateTime? PickedUpAt { get; private set; }

        /// <summary>
        /// Pre-order: the date/time (UTC) the customer wants to pick up the
        /// order. Null = ASAP. Set at checkout when the customer chooses
        /// "Đặt trước theo giờ".
        /// </summary>
        public DateTime? ScheduledFor { get; private set; }

        /// <summary>
        /// Sets or clears the internal staff note. Admin/staff only.
        /// </summary>
        public void SetAdminNote(string note)
        {
            var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (trimmed != null && trimmed.Length > AdminNoteMaxLength)
            {
                throw new DomainException($"Ghi chú nội bộ không được vượt quá {AdminNoteMaxLength} ký tự.");
            }

            AdminNote = trimmed;
        }

        /// <summary>
        /// Records (or clears) the customer-booked pickup shipper info.
        /// Allowed on ShipperPickup orders at any non-final status.
        /// </summary>
        public void SetPickupShipperInfo(string name, string phone)
        {
            var trimmedName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            var trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            if (trimmedName != null && trimmedName.Length > PickupShipperNameMaxLength)
            {
                throw new DomainException($"Tên shipper không được vượt quá {PickupShipperNameMaxLength} ký tự.");
            }

            if (trimmedPhone != null && trimmedPhone.Length > PickupShipperPhoneMaxLength)
            {
                throw new DomainException($"SĐT shipper không được vượt quá {PickupShipperPhoneMaxLength} ký tự.");
            }

            PickupShipperName = trimmedName;
            PickupShipperPhone = trimmedPhone;
        }

        /// <summary>
        /// Staff confirms the shipper/customer collected the order at the
        /// shop. Only for ShipperPickup orders that have not been picked up.
        /// </summary>
        public void MarkPickedUp()
        {
            if (FulfillmentMethod != FulfillmentMethod.ShipperPickup)
            {
                throw new DomainException("Chỉ đơn 'Shipper đến lấy' mới cần xác nhận lấy hàng.");
            }

            if (PickedUpAt.HasValue)
            {
                throw new DomainException("Đơn này đã được xác nhận lấy hàng.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Không thể xác nhận lấy hàng cho đơn đã hủy.");
            }

            if (Status == OrderStatus.Completed)
            {
                throw new DomainException("Đơn này đã hoàn thành.");
            }

            PickedUpAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Item 163: true when staff confirmed cash was collected for this
        /// order (COD / counter sales). False by default; set via the
        /// "💵 Đã thu tiền" button on the admin order list.
        /// </summary>
        public bool CashCollected { get; private set; }

        /// <summary>When cash collection was confirmed (UTC). Null until then.</summary>
        public DateTime? CashCollectedAt { get; private set; }

        /// <summary>
        /// Staff marks cash as collected. Idempotent guard: already-collected
        /// or cancelled orders are rejected. Typically used on COD orders,
        /// but the server only requires "not cancelled, not yet collected"
        /// so counter/POS flows keep working.
        /// </summary>
        public void MarkCashCollected()
        {
            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Không thể đánh dấu thu tiền cho đơn đã hủy.");
            }

            if (CashCollected)
            {
                throw new DomainException("Đơn này đã được đánh dấu thu tiền.");
            }

            CashCollected = true;
            CashCollectedAt = DateTime.UtcNow;
        }

        /// <summary>Thời điểm tạo đơn (UTC) — dùng sắp xếp, báo cáo theo ngày VN.</summary>
        public DateTime CreatedAt { get; private set; }

        private readonly List<OrderLine> _lines = new List<OrderLine>();

        /// <summary>
        /// Các dòng món của đơn (chỉ đọc từ ngoài — thêm/xóa dòng phải qua
        /// method của entity để giữ tổng tiền và trạng thái nhất quán).
        /// </summary>
        public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

        // EF Core
        private Order()
        {
        }

        private Order(
            string orderNumber,
            string customerName,
            string customerPhone,
            string customerEmail,
            string customerAddress,
            string notes,
            PaymentMethod paymentMethod,
            List<OrderLine> lines,
            Money deliveryFee,
            string discountCode,
            Money discountAmount,
            FulfillmentMethod fulfillmentMethod,
            Money giftFee = null,
            string taxCode = null,
            string companyName = null,
            string recipientName = null,
            string recipientPhone = null,
            bool isGift = false,
            string giftMessage = null,
            bool requiresDeposit = false,
            Money loyaltyDiscount = null,
            int loyaltyPointsRedeemed = 0,
            Money tierDiscount = null,
            string referralCodeUsed = null,
            string idempotencyKey = null,
            string promotionName = null,
            decimal promotionDiscountAmount = 0,
            Guid? branchId = null,
            string branchName = null,
            string source = null,
            string deliverySlot = null,
            string pickupShipperName = null,
            string pickupShipperPhone = null,
            DateTime? scheduledFor = null,
            Guid? customerUserId = null)
        {
            OrderNumber = orderNumber;
            CustomerName = customerName;
            CustomerPhone = customerPhone;
            CustomerPhoneNorm = ComputePhoneNorm(customerPhone);
            CustomerUserId = customerUserId;
            CustomerEmail = customerEmail;
            CustomerAddress = customerAddress;
            Notes = notes;
            Status = OrderStatus.Pending;
            PaymentMethod = paymentMethod;
            PaymentStatus = PaymentStatus.Unpaid;
            FulfillmentMethod = fulfillmentMethod;
            CreatedAt = DateTime.UtcNow;
            _lines.AddRange(lines);

            var currency = lines.First().LineTotal.Currency;
            var subtotal = lines.Sum(x => x.LineTotal.Amount);
            var fee = Math.Max(0, deliveryFee?.Amount ?? 0);
            var gift = Math.Max(0, giftFee?.Amount ?? 0);

            // Discounts are applied in priority order and never exceed the
            // remaining subtotal, so the total can never go negative.
            // The auto-applied "giờ vàng" promotion comes first.
            var remaining = subtotal;
            var promo = Math.Min(Math.Max(0, promotionDiscountAmount), remaining);
            remaining -= promo;
            var discount = Math.Min(Math.Max(0, discountAmount?.Amount ?? 0), remaining);
            remaining -= discount;
            var loyalty = Math.Min(Math.Max(0, loyaltyDiscount?.Amount ?? 0), remaining);
            remaining -= loyalty;
            var tier = Math.Min(Math.Max(0, tierDiscount?.Amount ?? 0), remaining);

            DeliveryFee = Money.Of(fee, currency);
            GiftFee = Money.Of(gift, currency);
            DiscountCode = string.IsNullOrWhiteSpace(discountCode) ? null : discountCode.Trim();
            DiscountAmount = Money.Of(discount, currency);
            LoyaltyDiscount = Money.Of(loyalty, currency);
            LoyaltyPointsRedeemed = loyaltyPointsRedeemed;
            TierDiscount = Money.Of(tier, currency);
            ReferralCodeUsed = string.IsNullOrWhiteSpace(referralCodeUsed) ? null : referralCodeUsed.Trim().ToUpperInvariant();
            TaxCode = string.IsNullOrWhiteSpace(taxCode) ? null : taxCode.Trim();
            CompanyName = string.IsNullOrWhiteSpace(companyName) ? null : companyName.Trim();
            RequiresDeposit = requiresDeposit;
            RecipientName = string.IsNullOrWhiteSpace(recipientName) ? null : recipientName.Trim();
            RecipientPhone = string.IsNullOrWhiteSpace(recipientPhone) ? null : recipientPhone.Trim();
            IsGift = isGift;
            GiftMessage = string.IsNullOrWhiteSpace(giftMessage) ? null : giftMessage.Trim();
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
            PromotionName = promo > 0 && !string.IsNullOrWhiteSpace(promotionName)
                ? promotionName.Trim()
                : null;
            PromotionDiscountAmount = promo;
            BranchId = branchId;
            BranchName = string.IsNullOrWhiteSpace(branchName) ? null : branchName.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? "Web" : source.Trim();
            DeliverySlot = string.IsNullOrWhiteSpace(deliverySlot) ? null : deliverySlot.Trim();
            SetPickupShipperInfo(pickupShipperName, pickupShipperPhone);
            ScheduledFor = scheduledFor;
            MoMoMatched = false;
            RefundStatus = RefundStatus.None;
            RefundAmount = 0;
            TotalAmount = Money.Of(subtotal - promo - discount - loyalty - tier + fee + gift, currency);
        }

        /// <summary>
        /// Factory tạo đơn hàng mới ở trạng thái Pending: validate toàn bộ
        /// đầu vào (trường bắt buộc, độ dài, định dạng email, địa chỉ khi giao
        /// hàng), rồi tính tổng tiền theo công thức duy nhất tại đây:
        /// Tổng = subtotal − promotion − discountCode − loyalty − tier + phí
        /// giao hàng + phí gói quà.
        /// </summary>
        /// <returns>Đơn hàng mới ở trạng thái Pending, tổng tiền đã chốt.</returns>
        /// <exception cref="DomainException">Ném khi thiếu/sai đầu vào (tên, SĐT, địa chỉ, email...).</exception>
        public static Order Create(
            string orderNumber,
            string customerName,
            string customerPhone,
            string customerEmail,
            string customerAddress,
            string notes,
            PaymentMethod paymentMethod,
            IEnumerable<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity,
                string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo, string Note)> lines,
            Money deliveryFee = null,
            string discountCode = null,
            Money discountAmount = null,
            FulfillmentMethod fulfillmentMethod = FulfillmentMethod.Delivery,
            Money giftFee = null,
            string taxCode = null,
            string companyName = null,
            string recipientName = null,
            string recipientPhone = null,
            bool isGift = false,
            string giftMessage = null,
            bool requiresDeposit = false,
            Money loyaltyDiscount = null,
            int loyaltyPointsRedeemed = 0,
            Money tierDiscount = null,
            string referralCodeUsed = null,
            string idempotencyKey = null,
            string promotionName = null,
            decimal promotionDiscountAmount = 0,
            Guid? branchId = null,
            string branchName = null,
            string source = null,
            string deliverySlot = null,
            string pickupShipperName = null,
            string pickupShipperPhone = null,
            DateTime? scheduledFor = null,
            Guid? customerUserId = null)
        {
            ValidateCreateInput(orderNumber, customerName, customerPhone, customerEmail,
                customerAddress, notes, fulfillmentMethod, deliverySlot);

            var orderLines = BuildOrderLines(lines);

            return new Order(
                orderNumber.Trim(),
                customerName.Trim(),
                customerPhone.Trim(),
                string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
                string.IsNullOrWhiteSpace(customerAddress) ? null : customerAddress.Trim(),
                notes?.Trim(),
                paymentMethod,
                orderLines,
                deliveryFee,
                discountCode,
                discountAmount,
                fulfillmentMethod,
                giftFee,
                taxCode,
                companyName,
                recipientName,
                recipientPhone,
                isGift,
                giftMessage,
                requiresDeposit,
                loyaltyDiscount,
                loyaltyPointsRedeemed,
                tierDiscount,
                referralCodeUsed,
                idempotencyKey,
                promotionName,
                promotionDiscountAmount,
                branchId,
                branchName,
                source,
                deliverySlot,
                pickupShipperName,
                pickupShipperPhone,
                scheduledFor,
                customerUserId);
        }

        /// <summary>
        /// Validate toàn bộ đầu vào của <see cref="Create"/>: trường bắt buộc,
        /// độ dài tối đa, định dạng email, và địa chỉ bắt buộc khi giao hàng
        /// (không bắt buộc khi khách tự đến lấy). Ném
        /// <see cref="DomainException"/> khi đầu vào thiếu hoặc sai.
        /// </summary>
        private static void ValidateCreateInput(
            string orderNumber,
            string customerName,
            string customerPhone,
            string customerEmail,
            string customerAddress,
            string notes,
            FulfillmentMethod fulfillmentMethod,
            string deliverySlot)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                throw new DomainException("Order number is required.");
            }

            if (orderNumber.Trim().Length > OrderNumberMaxLength)
            {
                throw new DomainException($"Order number must not exceed {OrderNumberMaxLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new DomainException("Customer name must not be empty.");
            }

            if (customerName.Trim().Length > CustomerNameMaxLength)
            {
                throw new DomainException($"Customer name must not exceed {CustomerNameMaxLength} characters.");
            }

            if (string.IsNullOrWhiteSpace(customerPhone))
            {
                throw new DomainException("Customer phone must not be empty.");
            }

            if (customerPhone.Trim().Length > CustomerPhoneMaxLength)
            {
                throw new DomainException($"Customer phone must not exceed {CustomerPhoneMaxLength} characters.");
            }

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var email = customerEmail.Trim();
                if (email.Length > CustomerEmailMaxLength || !IsValidEmail(email))
                {
                    throw new DomainException("Customer email is not a valid email address.");
                }
            }

            var isPickup = fulfillmentMethod == FulfillmentMethod.Pickup
                || fulfillmentMethod == FulfillmentMethod.ShipperPickup;

            if (!isPickup && string.IsNullOrWhiteSpace(customerAddress))
            {
                throw new DomainException("Customer address must not be empty.");
            }

            if (!string.IsNullOrWhiteSpace(customerAddress) && customerAddress.Trim().Length > CustomerAddressMaxLength)
            {
                throw new DomainException($"Customer address must not exceed {CustomerAddressMaxLength} characters.");
            }

            if (notes != null && notes.Length > NotesMaxLength)
            {
                throw new DomainException($"Order notes must not exceed {NotesMaxLength} characters.");
            }

            if (deliverySlot != null && deliverySlot.Trim().Length > DeliverySlotMaxLength)
            {
                throw new DomainException($"Delivery slot must not exceed {DeliverySlotMaxLength} characters.");
            }
        }

        /// <summary>
        /// Dựng danh sách <see cref="OrderLine"/> từ tuple đầu vào, đồng thời
        /// đảm bảo đơn có ít nhất một dòng và mọi dòng dùng chung một loại tiền tệ.
        /// </summary>
        private static List<OrderLine> BuildOrderLines(
            IEnumerable<(Guid ProductId, string ProductName, Money UnitPrice, int Quantity,
                string SizeName, string ToppingNames, decimal ToppingsExtra, bool IsCombo, string Note)> lines)
        {
            var orderLines = (lines ?? Enumerable.Empty<(Guid, string, Money, int, string, string, decimal, bool, string)>())
                .Select(x => OrderLine.Create(
                    x.Item1, x.Item2, x.Item3, x.Item4, x.Item5, x.Item6, x.Item7, x.Item8, x.Item9))
                .ToList();

            if (orderLines.Count == 0)
            {
                throw new DomainException("Order must contain at least one line.");
            }

            var currency = orderLines.First().UnitPrice.Currency;
            if (orderLines.Any(x => x.UnitPrice.Currency != currency))
            {
                throw new DomainException("All order lines must use the same currency.");
            }

            return orderLines;
        }

        /// <summary>
        /// Marks the post-checkout lucky spin as used. Only one spin per order.
        /// </summary>
        public void MarkLuckySpinUsed()
        {
            LuckySpinUsed = true;
        }

        /// <summary>
        /// Item 128: flags the order as auto-matched by the MoMo IPN callback
        /// (the callback's OrderInfo contained this order's number).
        /// </summary>
        public void MarkMoMoMatched()
        {
            MoMoMatched = true;
        }

        /// <summary>
        /// Item 130: admin requests a refund (records the intent; the money
        /// movement is manual — cash / bank / wallet credit).
        /// </summary>
        public void RequestRefund(decimal amount, string reason)
        {
            if (RefundStatus != RefundStatus.None)
            {
                throw new DomainException("Đã có yêu cầu hoàn tiền cho đơn này.");
            }

            if (amount <= 0)
            {
                throw new DomainException("Số tiền hoàn phải lớn hơn 0.");
            }

            if (amount > TotalAmount.Amount)
            {
                throw new DomainException("Số tiền hoàn không được vượt quá tổng đơn.");
            }

            RefundStatus = RefundStatus.Requested;
            RefundAmount = amount;
            if (!string.IsNullOrWhiteSpace(reason))
            {
                AddRewardNote("Hoàn tiền: " + reason.Trim());
            }
        }

        /// <summary>
        /// Item 130: records the refund as paid out (money moved manually).
        /// </summary>
        public void ConfirmRefund()
        {
            if (RefundStatus != RefundStatus.Requested)
            {
                throw new DomainException("Chưa có yêu cầu hoàn tiền nào đang chờ.");
            }

            RefundStatus = RefundStatus.Refunded;
        }

        /// <summary>
        /// Assigns (or unassigns with null) a delivery shipper.
        /// </summary>
        public void AssignShipper(Guid? shipperId)
        {
            ShipperId = shipperId;
        }

        /// <summary>
        /// Item 142: merges every line of another order into this one
        /// (admin "gộp đơn trùng SĐT"). Only allowed while this order is
        /// still Pending or Confirmed. Discounts stay as fixed amounts;
        /// totals are recalculated server-side.
        /// </summary>
        public void MergeLinesFrom(Order source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source.Id == Id)
            {
                throw new DomainException("Không thể gộp một đơn vào chính nó.");
            }

            if (Status != OrderStatus.Pending && Status != OrderStatus.Confirmed)
            {
                throw new DomainException("Chỉ gộp được vào đơn đang chờ xác nhận hoặc đã xác nhận.");
            }

            if (source.Lines == null || source.Lines.Count == 0)
            {
                throw new DomainException("Đơn được gộp không có món nào.");
            }

            foreach (var line in source.Lines)
            {
                _lines.Add(OrderLine.Create(
                    line.ProductId,
                    line.ProductName,
                    line.UnitPrice,
                    line.Quantity,
                    line.SizeName,
                    line.ToppingNames,
                    line.ToppingsExtra,
                    line.IsCombo,
                    line.Note));
            }

            RecalculateTotals();
        }

        /// <summary>
        /// Appends an auto-reward note (e.g. free topping) to the order notes.
        /// </summary>
        public void AddRewardNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return;
            }

            var trimmed = note.Trim();
            Notes = string.IsNullOrWhiteSpace(Notes) ? trimmed : Notes + "\n" + trimmed;
        }

        /// <summary>
        /// Customer edits quantities of a still-Pending order (track-order
        /// "Sửa đơn"). Totals are recalculated server-side; discounts already
        /// applied stay as fixed amounts.
        /// </summary>
        public void UpdateLineQuantities(IEnumerable<(Guid LineId, int Quantity)> updates)
        {
            if (Status != OrderStatus.Pending)
            {
                throw new DomainException("Chỉ đơn hàng đang chờ xác nhận mới được sửa.");
            }

            var map = (updates ?? Enumerable.Empty<(Guid, int)>())
                .GroupBy(x => x.Item1)
                .ToDictionary(g => g.Key, g => g.Last().Item2);

            if (map.Count == 0)
            {
                throw new DomainException("Không có thay đổi nào.");
            }

            foreach (var line in _lines)
            {
                if (!map.TryGetValue(line.Id, out var quantity))
                {
                    continue;
                }

                if (quantity < 0)
                {
                    throw new DomainException("Số lượng không hợp lệ.");
                }

                line.SetQuantity(quantity);
            }

            _lines.RemoveAll(x => x.Quantity == 0);

            if (_lines.Count == 0)
            {
                throw new DomainException("Đơn hàng phải có ít nhất một món.");
            }

            RecalculateTotals();
        }

        /// <summary>
        /// Customer self-service: changes the delivery address / time slot of
        /// a still-Pending order (track-order "Đổi địa chỉ"). Only address and
        /// slot — totals are unaffected.
        /// </summary>
        public void UpdateAddressByCustomer(string customerAddress, string deliverySlot)
        {
            if (Status != OrderStatus.Pending)
            {
                throw new DomainException("Chỉ đơn hàng đang chờ xác nhận mới được đổi địa chỉ / giờ giao.");
            }

            var address = (customerAddress ?? string.Empty).Trim();
            var slot = (deliverySlot ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(address))
            {
                throw new DomainException("Vui lòng nhập địa chỉ nhận hàng.");
            }

            if (address.Length > CustomerAddressMaxLength)
            {
                throw new DomainException($"Địa chỉ không được vượt quá {CustomerAddressMaxLength} ký tự.");
            }

            if (slot.Length > DeliverySlotMaxLength)
            {
                throw new DomainException($"Giờ giao không được vượt quá {DeliverySlotMaxLength} ký tự.");
            }

            CustomerAddress = address;
            DeliverySlot = string.IsNullOrEmpty(slot) ? null : slot;
        }

        /// <summary>
        /// Item 138: khách tự sửa SĐT người nhận + ghi chú khi đơn còn Pending.
        /// </summary>
        public void UpdateContactByCustomer(string recipientPhone, string notes)
        {
            if (Status != OrderStatus.Pending)
            {
                throw new DomainException("Chỉ đơn hàng đang chờ xác nhận mới được sửa thông tin liên hệ.");
            }

            var phone = (recipientPhone ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(phone))
            {
                // Validate SĐT VN: 0 + (3|5|7|8|9) + 8 số.
                if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^(0)(3|5|7|8|9)\d{8}$"))
                {
                    throw new DomainException("SĐT không đúng định dạng (VD: 0901234567).");
                }

                if (phone.Length > RecipientPhoneMaxLength)
                {
                    throw new DomainException($"SĐT không được vượt quá {RecipientPhoneMaxLength} ký tự.");
                }

                RecipientPhone = phone;
            }

            var note = (notes ?? string.Empty).Trim();
            Notes = string.IsNullOrEmpty(note) ? null : note;
        }

        private void RecalculateTotals()
        {
            var currency = TotalAmount.Currency;
            var subtotal = _lines.Sum(x => x.LineTotal.Amount);
            var promo = Math.Min(PromotionDiscountAmount, subtotal);
            var discount = Math.Min(DiscountAmount.Amount, subtotal - promo);
            var loyalty = Math.Min(LoyaltyDiscount.Amount, subtotal - promo - discount);
            var tier = Math.Min(TierDiscount.Amount, subtotal - promo - discount - loyalty);
            PromotionDiscountAmount = promo;
            DiscountAmount = Money.Of(discount, currency);
            LoyaltyDiscount = Money.Of(loyalty, currency);
            TierDiscount = Money.Of(tier, currency);
            TotalAmount = Money.Of(subtotal - promo - discount - loyalty - tier + DeliveryFee.Amount + GiftFee.Amount, currency);
        }

        /// <summary>
        /// Minimal email shape check (the DTO layer also validates with
        /// <c>[EmailAddress]</c>). Kept dependency-free in the domain.
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex != email.LastIndexOf('@'))
            {
                return false;
            }

            var dot = email.LastIndexOf('.');
            return dot > atIndex + 1 && dot < email.Length - 1 && !email.Contains(' ');
        }

        /// <summary>
        /// Tính <see cref="CustomerPhoneNorm"/>: chỉ giữ chữ số, lấy 9 số
        /// cuối; SĐT ít hơn 9 số thì giữ nguyên chuỗi số. Dùng chung chuẩn
        /// khoan dung với PhoneNumber.PhonesMatch — cùng 1 số dù khách
        /// gõ "0912 345 678", "+84912345678" hay "0912345678".
        /// </summary>
        private static string ComputePhoneNorm(string customerPhone)
        {
            var digits = new string((customerPhone ?? string.Empty).Where(char.IsDigit).ToArray());
            return digits.Length > CustomerPhoneNormMaxLength
                ? digits.Substring(digits.Length - CustomerPhoneNormMaxLength)
                : digits;
        }

        /// <summary>
        /// Chuyển đơn sang Đã xác nhận. Chỉ hợp lệ từ Chờ xác nhận — phải đi
        /// từng bước (không nhảy cóc) vì mỗi bước chuyển ở tầng application
        /// đều ghi audit lịch sử, log nhân viên và webhook, và chỉ bước Hoàn
        /// tất mới cộng điểm loyalty.
        /// </summary>
        /// <exception cref="DomainException">Ném khi đơn không ở trạng thái Chờ xác nhận.</exception>
        public void Confirm()
        {
            EnsureTransition(OrderStatus.Pending, nameof(Confirm));
            Status = OrderStatus.Confirmed;
        }

        /// <summary>
        /// Chuyển đơn sang Đang chuẩn bị. Chỉ hợp lệ từ Đã xác nhận.
        /// </summary>
        /// <exception cref="DomainException">Ném khi đơn không ở trạng thái Đã xác nhận.</exception>
        public void StartPreparing()
        {
            EnsureTransition(OrderStatus.Confirmed, nameof(StartPreparing));
            Status = OrderStatus.Preparing;
        }

        /// <summary>
        /// Chuyển đơn sang Đang giao. Chỉ hợp lệ từ Đang chuẩn bị.
        /// </summary>
        /// <exception cref="DomainException">Ném khi đơn không ở trạng thái Đang chuẩn bị.</exception>
        public void StartDelivering()
        {
            EnsureTransition(OrderStatus.Preparing, nameof(StartDelivering));
            Status = OrderStatus.Delivering;
        }

        /// <summary>
        /// Hoàn tất đơn. Chỉ hợp lệ từ Đang giao — đây là bước duy nhất cộng
        /// điểm loyalty và chạy quy tắc thưởng ở tầng application.
        /// </summary>
        /// <exception cref="DomainException">Ném khi đơn không ở trạng thái Đang giao.</exception>
        public void Complete()
        {
            EnsureTransition(OrderStatus.Delivering, nameof(Complete));
            Status = OrderStatus.Completed;
        }

        /// <summary>
        /// Hủy đơn. Không được hủy đơn đã hoàn tất; hủy đơn đã hủy thì báo lỗi
        /// (idempotent-unsafe để phát hiện gọi trùng bất thường).
        /// </summary>
        /// <exception cref="DomainException">Ném khi đơn đã hoàn tất hoặc đã hủy.</exception>
        public void Cancel()
        {
            if (Status == OrderStatus.Completed)
            {
                throw new DomainException("A completed order cannot be cancelled.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Order is already cancelled.");
            }

            Status = OrderStatus.Cancelled;
        }

        /// <summary>
        /// Item 233: lý do hủy đơn (khách chọn hoặc nhân viên chọn khi hủy).
        /// Null với đơn chưa hủy hoặc đơn hủy bởi hệ thống không ghi lý do.
        /// </summary>
        public string CancelReason { get; private set; }

        /// <summary>
        /// Item 233: ghi lý do hủy (chuẩn hóa trim, cắt tối đa
        /// <see cref="CancelReasonMaxLength"/> ký tự).
        /// </summary>
        public void SetCancelReason(string reason)
        {
            CancelReason = string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim()[..Math.Min(reason.Trim().Length, CancelReasonMaxLength)];
        }

        /// <summary>
        /// Customer reports they sent the bank transfer. Only for bank-transfer
        /// orders that are still unpaid. Admin confirms with <see cref="ConfirmPayment"/>.
        /// </summary>
        public void MarkTransferred()
        {
            if (PaymentMethod != PaymentMethod.BankTransfer)
            {
                throw new DomainException("Only bank-transfer orders can be marked as transferred.");
            }

            if (PaymentStatus != PaymentStatus.Unpaid)
            {
                throw new DomainException($"Cannot mark payment as transferred. Current payment status: '{PaymentStatus}'.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Cannot mark payment for a cancelled order.");
            }

            PaymentStatus = PaymentStatus.PendingConfirmation;
        }

        /// <summary>
        /// Payment confirmed — by the admin (bank transfer) or by the MoMo callback.
        /// Allowed from Unpaid (MoMo, COD collected), PendingConfirmation (bank
        /// transfer) or Failed (lần thanh toán trước thất bại, khách thanh toán
        /// lại thành công — IPN resultCode=0 sau khi đã có callback thất bại).
        /// </summary>
        public void ConfirmPayment()
        {
            if (PaymentStatus == PaymentStatus.Paid)
            {
                throw new DomainException("Payment is already confirmed.");
            }

            if (PaymentStatus != PaymentStatus.Unpaid
                && PaymentStatus != PaymentStatus.PendingConfirmation
                && PaymentStatus != PaymentStatus.Failed)
            {
                throw new DomainException($"Cannot confirm payment. Current payment status: '{PaymentStatus}'.");
            }

            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException("Cannot confirm payment for a cancelled order.");
            }

            PaymentStatus = PaymentStatus.Paid;
        }

        /// <summary>
        /// Payment failed (MoMo reported failure). Allowed from Unpaid / PendingConfirmation.
        /// </summary>
        public void MarkPaymentFailed()
        {
            if (PaymentStatus == PaymentStatus.Paid)
            {
                throw new DomainException("A paid order cannot be marked as payment failed.");
            }

            if (PaymentStatus != PaymentStatus.Unpaid && PaymentStatus != PaymentStatus.PendingConfirmation)
            {
                throw new DomainException($"Cannot mark payment as failed. Current payment status: '{PaymentStatus}'.");
            }

            PaymentStatus = PaymentStatus.Failed;
        }

        private void EnsureTransition(OrderStatus expectedStatus, string action)
        {
            if (Status == OrderStatus.Cancelled)
            {
                throw new DomainException($"Cannot {action} a cancelled order.");
            }

            if (Status != expectedStatus)
            {
                throw new DomainException($"Order must be in '{expectedStatus}' status to {action}. Current status: '{Status}'.");
            }
        }
    }
}
