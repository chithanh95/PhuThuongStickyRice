using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace PhuThuongStickyRice.Application.Orders.DTOs
{
    public class OrderLineDto
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }

        public string SizeName { get; set; }

        public string ToppingNames { get; set; }

        public decimal ToppingsExtra { get; set; }

        public bool IsCombo { get; set; }

        /// <summary>Per-item note from the customer.</summary>
        public string Note { get; set; }
    }

    public class OrderDto : EntityDto<Guid>
    {
        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        /// <summary>
        /// Item 149: tags of this customer's phone (e.g. VIP) so staff see
        /// them while processing the order. Admin endpoints only.
        /// </summary>
        public List<PhuThuongStickyRice.Application.Customer.CustomerTagDto> CustomerTags { get; set; }
            = new List<PhuThuongStickyRice.Application.Customer.CustomerTagDto>();

        /// <summary>
        /// Item 130 (đúng): true when this order's customer is marked VIP —
        /// set together with <see cref="CustomerTags"/> on admin endpoints
        /// only. Kitchen and order list use it for priority display/sorting.
        /// </summary>
        public bool IsVip { get; set; }

        public string Notes { get; set; }

        /// <summary>
        /// Wave 6c (item 33): internal staff note. Serialized only on
        /// admin endpoints — public methods (track/cancel/edit lines,
        /// mark-transferred) null it out before returning.
        /// </summary>
        public string AdminNote { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        /// <summary>"Delivery", "Pickup" or "ShipperPickup".</summary>
        public string FulfillmentMethod { get; set; }

        /// <summary>
        /// Name of the customer-booked shipper (Grab/Ahamove/...) for a
        /// ShipperPickup order. Nullable.
        /// </summary>
        public string PickupShipperName { get; set; }

        /// <summary>Phone of the customer-booked shipper. Nullable.</summary>
        public string PickupShipperPhone { get; set; }

        /// <summary>
        /// When the shipper/customer collected a ShipperPickup order at the
        /// shop (UTC). Null until staff confirms pickup.
        /// </summary>
        public DateTime? PickedUpAt { get; set; }

        /// <summary>
        /// Pre-order scheduled pickup time (UTC). Null = nhận ngay (ASAP).
        /// </summary>
        public DateTime? ScheduledFor { get; set; }

        /// <summary>
        /// Item 163: true when staff confirmed cash was collected (COD).
        /// </summary>
        public bool CashCollected { get; set; }

        /// <summary>When cash collection was confirmed (UTC).</summary>
        public DateTime? CashCollectedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }

        /// <summary>Lines subtotal before discount and fee.</summary>
        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public string DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        /// <summary>Loyalty-points discount in VND.</summary>
        public decimal LoyaltyDiscount { get; set; }

        public int LoyaltyPointsRedeemed { get; set; }

        /// <summary>Membership-tier discount in VND.</summary>
        public decimal TierDiscount { get; set; }

        public string ReferralCodeUsed { get; set; }

        /// <summary>Optional VAT invoice tax code.</summary>
        public string TaxCode { get; set; }

        /// <summary>Optional VAT invoice company name.</summary>
        public string CompanyName { get; set; }

        /// <summary>True when a bank-transfer deposit is required.</summary>
        public bool RequiresDeposit { get; set; }

        public string RecipientName { get; set; }

        public string RecipientPhone { get; set; }

        public bool IsGift { get; set; }

        public string GiftMessage { get; set; }

        public decimal GiftFee { get; set; }

        public bool LuckySpinUsed { get; set; }

        /// <summary>Assigned shipper id (nullable).</summary>
        public Guid? ShipperId { get; set; }

        /// <summary>Assigned shipper name.</summary>
        public string ShipperName { get; set; }

        /// <summary>Branch id (nullable, item 109).</summary>
        public Guid? BranchId { get; set; }

        /// <summary>Branch display name.</summary>
        public string BranchName { get; set; }

        /// <summary>Auto-applied "giờ vàng" promotion name, if any.</summary>
        public string PromotionName { get; set; }

        /// <summary>Auto-applied promotion discount in VND.</summary>
        public decimal PromotionDiscountAmount { get; set; }

        /// <summary>Order source (item 133): "Web", "QR bàn", "Link chia sẻ", "Nhóm", "POS".</summary>
        public string Source { get; set; }

        /// <summary>Wave 6a (item 23): scheduled delivery slot, e.g. "06:00–06:30". Null = giao ngay.</summary>
        public string DeliverySlot { get; set; }

        /// <summary>Item 128: MoMo IPN auto-matched by parsing OrderInfo.</summary>
        public bool MoMoMatched { get; set; }

        /// <summary>Item 130: refund state ("None" / "Requested" / "Refunded").</summary>
        public string RefundStatus { get; set; }

        /// <summary>Item 130: refund amount in VND.</summary>
        public decimal RefundAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();
    }

    public class CreateOrderLineDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>Optional size option id (Nhỏ / Vừa / Lớn).</summary>
        public Guid? SizeOptionId { get; set; }

        /// <summary>Optional topping ids.</summary>
        public List<Guid> ToppingIds { get; set; } = new List<Guid>();

        /// <summary>True when ProductId is a combo id, not a product id.</summary>
        public bool IsCombo { get; set; }

        /// <summary>Optional per-item note (e.g. "ít hành"). Rendered on sticky labels.</summary>
        [StringLength(500)]
        public string Note { get; set; }
    }

    public class CreateOrderDto
    {
        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }

        /// <summary>
        /// Optional. When provided, the order confirmation email is sent here.
        /// </summary>
        [EmailAddress]
        [StringLength(254)]
        public string CustomerEmail { get; set; }

        /// <summary>
        /// Delivery address. Required only for legacy Delivery orders;
        /// ShipperPickup/Pickup orders leave it empty.
        /// </summary>
        [StringLength(500)]
        public string CustomerAddress { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        /// <summary>Payment method: "Cod" (default), "BankTransfer", "MoMo" or "Wallet".</summary>
        public string PaymentMethod { get; set; }

        /// <summary>Optional discount/promo code entered at checkout.</summary>
        [StringLength(50)]
        public string DiscountCode { get; set; }

        /// <summary>
        /// "ShipperPickup" (customer-booked shipper collects at the shop),
        /// "Pickup" (customer collects in person), or legacy "Delivery".
        /// Defaults to ShipperPickup.
        /// </summary>
        public string FulfillmentMethod { get; set; }

        /// <summary>Optional VAT invoice tax code.</summary>
        [StringLength(30)]
        public string TaxCode { get; set; }

        /// <summary>Optional VAT invoice company name.</summary>
        [StringLength(300)]
        public string CompanyName { get; set; }

        /// <summary>Recipient name when ordering for someone else.</summary>
        [StringLength(200)]
        public string RecipientName { get; set; }

        /// <summary>Recipient phone when ordering for someone else.</summary>
        [StringLength(30)]
        public string RecipientPhone { get; set; }

        /// <summary>Gift packaging requested.</summary>
        public bool IsGift { get; set; }

        [StringLength(1000)]
        public string GiftMessage { get; set; }

        /// <summary>Loyalty points the customer wants to redeem.</summary>
        [Range(0, int.MaxValue)]
        public int RedeemPoints { get; set; }

        /// <summary>Referrer's referral code, if any.</summary>
        [StringLength(20)]
        public string ReferralCode { get; set; }

        /// <summary>Optional birthday (updates the loyalty profile).</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Idempotency key for the checkout attempt. Retrying with the same
        /// key within 24h returns the existing order.
        /// </summary>
        [StringLength(100)]
        public string IdempotencyKey { get; set; }

        /// <summary>reCAPTCHA v3 token. Verified when reCAPTCHA is enabled.</summary>
        public string RecaptchaToken { get; set; }

        /// <summary>
        /// Wave 6a (item 23): scheduled delivery slot ("06:00–06:30").
        /// Null/empty = giao ngay.
        /// </summary>
        [StringLength(50)]
        public string DeliverySlot { get; set; }

        /// <summary>Optional branch id (item 109). Null = no branch.</summary>
        public Guid? BranchId { get; set; }

        /// <summary>
        /// Name of the customer-booked shipper (Grab/Ahamove/...) for a
        /// ShipperPickup order. Optional — may be left empty at checkout.
        /// </summary>
        [StringLength(200)]
        public string PickupShipperName { get; set; }

        /// <summary>
        /// Phone of the customer-booked shipper for a ShipperPickup order.
        /// Optional.
        /// </summary>
        [StringLength(30)]
        public string PickupShipperPhone { get; set; }

        /// <summary>
        /// Pre-order scheduled pickup time (UTC). Null = nhận ngay (ASAP).
        /// Must be at least 1 hour in the future and within shop open hours.
        /// </summary>
        public DateTime? ScheduledFor { get; set; }

        /// <summary>
        /// Order source (item 133): "Web", "QR bàn", "Link chia sẻ", "Nhóm",
        /// "POS". Defaults to "Web" when empty.
        /// </summary>
        [StringLength(50)]
        public string Source { get; set; }

        public List<CreateOrderLineDto> Lines { get; set; } = new List<CreateOrderLineDto>();
    }

    /// <summary>Customer self-service: cancel a Pending order.</summary>
    public class CancelOrderByCustomerInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        /// <summary>
        /// Item 233: lý do hủy đơn — bắt buộc chọn (Đổi ý / Đặt nhầm món /
        /// Chờ quá lâu / Món hết hàng / Lý do khác).
        /// </summary>
        [Required]
        public string CancelReason { get; set; }
    }

    public class UpdateOrderLineQtyDto
    {
        [Required]
        public Guid LineId { get; set; }

        /// <summary>New quantity. 0 removes the line.</summary>
        [Range(0, 100)]
        public int Quantity { get; set; }
    }

    /// <summary>Customer self-service: edit quantities of a Pending order.</summary>
    public class UpdateOrderLinesInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        [Required]
        public List<UpdateOrderLineQtyDto> Lines { get; set; } = new List<UpdateOrderLineQtyDto>();
    }

    /// <summary>Customer self-service: change address / delivery slot of a Pending order.</summary>
    public class UpdateOrderAddressByCustomerInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        [Required]
        public string CustomerAddress { get; set; }

        public string DeliverySlot { get; set; }
    }

    /// <summary>Item 138: customer self-service — update recipient phone + notes.</summary>
    public class UpdateOrderContactByCustomerInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        public string RecipientPhone { get; set; }

        public string Notes { get; set; }
    }

    /// <summary>Post-delivery survey for a completed order.</summary>
    public class SubmitReviewInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        /// <summary>Optional photo URL uploaded via track/review/photo.</summary>
        [StringLength(500)]
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Wave 6b (item 26): optional per-dish star ratings. Each entry
        /// must reference a product of the order; entries for products not
        /// in the order are ignored server-side.
        /// </summary>
        public List<SubmitReviewLineInput> Lines { get; set; } = new List<SubmitReviewLineInput>();
    }

    /// <summary>Wave 6b (item 26): per-dish star rating inside the survey.</summary>
    public class SubmitReviewLineInput
    {
        public Guid ProductId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }
    }

    public class OrderReviewLineDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int Rating { get; set; }
    }

    public class OrderReviewDto
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        /// <summary>Optional photo attached to the review.</summary>
        public string PhotoUrl { get; set; }

        /// <summary>Auto-generated reward voucher code (photo reviews only).</summary>
        public string RewardCode { get; set; }

        /// <summary>Hidden reviews are not shown publicly.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Admin reply, shown publicly under the review.</summary>
        public string AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>Wave 6b (item 26): per-dish star ratings.</summary>
        public List<OrderReviewLineDto> Lines { get; set; } = new List<OrderReviewLineDto>();
    }

    /// <summary>Admin: hide/unhide a review.</summary>
    public class SetReviewHiddenDto
    {
        public bool IsHidden { get; set; }
    }

    /// <summary>Admin: reply to a review.</summary>
    public class ReplyReviewDto
    {
        [StringLength(1000)]
        public string Reply { get; set; }
    }

    /// <summary>Item 52: customer rates the shipper (1-5) for an order review.
    /// CustomerPhone proves ownership of the review (no login on this flow).</summary>
    public class SetShipperRatingDto
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        /// <summary>SĐT đặt hàng của review — dùng để xác thực quyền đánh giá.</summary>
        [Required]
        [StringLength(30)]
        public string CustomerPhone { get; set; }
    }

    /// <summary>Item 87: public review shown on the landing page.
    /// Never contains Id, CustomerPhone (raw), OrderNumber or RewardCode —
    /// only the server-masked phone so voucher codes cannot be stolen.</summary>
    public class PublicOrderReviewDto
    {
        /// <summary>Masked phone, e.g. "090***67" — safe to display publicly.</summary>
        public string MaskedCustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public string PhotoUrl { get; set; }

        public string AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<OrderReviewLineDto> Lines { get; set; } = new List<OrderReviewLineDto>();
    }

    /// <summary>Lucky-spin request after checkout.</summary>
    public class SpinInput
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public string CustomerPhone { get; set; }
    }

    public class SpinResultDto
    {
        /// <summary>Segment key: Percent5, Percent10, Percent15, FreeShip, Cash20k, Miss.</summary>
        public string Segment { get; set; }

        public string PrizeLabel { get; set; }

        /// <summary>Generated single-use code; null when the segment is "Miss".</summary>
        public string Code { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Public: last order info for a phone number, used to auto-fill the
    /// checkout address. Returns only name/address — no sensitive data.
    /// </summary>
    public class LastOrderInfoDto
    {
        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public DateTime LastOrderAt { get; set; }
    }

    /// <summary>Customer aggregated by phone number (admin).</summary>
    public class CustomerDto
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public long OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime LastOrderAt { get; set; }

        /// <summary>Item 149: tags attached to this customer phone.</summary>
        public List<PhuThuongStickyRice.Application.Customer.CustomerTagDto> Tags { get; set; }
            = new List<PhuThuongStickyRice.Application.Customer.CustomerTagDto>();

        /// <summary>
        /// Item 130 (đúng): true when the phone carries the VIP tag.
        /// </summary>
        public bool IsVip { get; set; }
    }

    public class GetOrdersInput : PagedAndSortedResultRequestDto
    {
        // Nullable: filter không bắt buộc, query string vắng mặt => null.
        public string? SearchTerm { get; set; }

        // Nullable: filter không bắt buộc, query string vắng mặt => null.
        public string? Status { get; set; }

        /// <summary>Filter by assigned shipper.</summary>
        public Guid? ShipperId { get; set; }

        /// <summary>
        /// Wave 6c (item 35): date-only range filter on the order creation
        /// day (Vietnam timezone day boundaries).
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Wave 6c (item 35): date-only range filter on the order creation
        /// day (Vietnam timezone day boundaries).
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Item 163: filter by cash-collection state. true = đã thu,
        /// false = chưa thu, null = tất cả.
        /// </summary>
        public bool? CashCollected { get; set; }

        /// <summary>
        /// Item 130 (đúng): true = chỉ hiện đơn của khách VIP.
        /// </summary>
        public bool VipOnly { get; set; }
    }

    /// <summary>Wave 6c (item 33): admin edits the internal order note.</summary>
    public class UpdateAdminNoteDto
    {
        /// <summary>Internal note; null/empty clears it.</summary>
        [StringLength(2000)]
        public string Note { get; set; }
    }

    /// <summary>
    /// Staff confirms a ShipperPickup order was collected at the shop.
    /// Optionally records/updates the customer-booked shipper info.
    /// </summary>
    public class MarkPickedUpDto
    {
        /// <summary>Shipper name; null/empty clears it.</summary>
        [StringLength(200)]
        public string PickupShipperName { get; set; }

        /// <summary>Shipper phone; null/empty clears it.</summary>
        [StringLength(30)]
        public string PickupShipperPhone { get; set; }
    }

    /// <summary>Wave 6c (item 34): one recorded status transition.</summary>
    public class OrderStatusHistoryDto
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string FromStatus { get; set; }

        public string ToStatus { get; set; }

        /// <summary>UTC timestamp of the transition.</summary>
        public DateTime At { get; set; }

        public string ByUser { get; set; }
    }

    /// <summary>Wave 6c (item 31): one payment channel of the daily reconciliation.</summary>
    public class ReconciliationChannelDto
    {
        public string PaymentMethod { get; set; }

        public string Label { get; set; }

        public long OrderCount { get; set; }

        /// <summary>Revenue of non-cancelled orders.</summary>
        public decimal Revenue { get; set; }

        /// <summary>Revenue already paid (PaymentStatus = Paid).</summary>
        public decimal PaidAmount { get; set; }

        /// <summary>Revenue not yet paid (Unpaid / PendingConfirmation / Failed).</summary>
        public decimal UnpaidAmount { get; set; }
    }

    /// <summary>Wave 6c (item 31): daily payment-channel reconciliation.</summary>
    public class DailyReconciliationDto
    {
        /// <summary>The reconciled day (Vietnam timezone, date-only).</summary>
        public DateTime Date { get; set; }

        public List<ReconciliationChannelDto> Channels { get; set; } = new List<ReconciliationChannelDto>();

        public long TotalOrderCount { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal TotalPaidAmount { get; set; }

        public decimal TotalUnpaidAmount { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        public string Status { get; set; }

        /// <summary>
        /// Item 233: lý do hủy — nhân viên chọn khi chuyển sang Cancelled.
        /// Bỏ trống = giữ nguyên lý do cũ (nếu có).
        /// </summary>
        public string CancelReason { get; set; }
    }

    /// <summary>Admin: assign/unassign a delivery shipper.</summary>
    public class AssignShipperDto
    {
        /// <summary>Shipper id; null unassigns.</summary>
        public Guid? ShipperId { get; set; }
    }

    /// <summary>Customer marks their bank transfer as sent. Phone must match the order.</summary>
    public class MarkTransferredDto
    {
        [Required]
        public string CustomerPhone { get; set; }
    }

    /// <summary>Public order lookup: order number + phone (the phone proves ownership).</summary>
    public class TrackOrderInput
    {
        [Required]
        public string OrderNumber { get; set; }

        [Required]
        public string CustomerPhone { get; set; }
    }

    /// <summary>Public bank-transfer details shown at checkout (account info + VietQR).</summary>
    public class BankTransferInfoDto
    {
        public string BankName { get; set; }

        public string BankId { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public string QrTemplate { get; set; }
    }

    /// <summary>Response of POST /api/orders/{id}/momo/create.</summary>
    public class MoMoCreatePaymentResultDto
    {
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public decimal Amount { get; set; }

        /// <summary>
        /// Where the buyer is sent to pay. In TestMode this is the in-app
        /// MoMo simulator page; in production it is the real MoMo payUrl.
        /// </summary>
        public string PayUrl { get; set; }

        public bool TestMode { get; set; }
    }

    /// <summary>
    /// MoMo return / IPN payload. In TestMode the simulator posts the echoed
    /// create-signature (<see cref="SimulatedSignature"/>) plus the result the
    /// buyer picked; in production MoMo posts its signed IPN fields.
    /// </summary>
    public class MoMoCallbackDto
    {
        public string OrderId { get; set; }

        public string RequestId { get; set; }

        public long Amount { get; set; }

        public string OrderInfo { get; set; }

        public int ResultCode { get; set; }

        public string Message { get; set; }

        public string TransId { get; set; }

        public string PayType { get; set; }

        public long ResponseTime { get; set; }

        public string ExtraData { get; set; }

        public string Signature { get; set; }

        /// <summary>Echoed create-signature, only used by the TEST simulator.</summary>
        public string SimulatedSignature { get; set; }

        public bool Simulated { get; set; }
    }

    public class DashboardStatsDto
    {
        public long TotalOrders { get; set; }

        public long PendingOrders { get; set; }

        /// <summary>
        /// Item 86: Pending orders older than the configured
        /// UnconfirmedAlertMinutes — shown as an urgent dashboard alert.
        /// </summary>
        public long StalePendingOrders { get; set; }

        /// <summary>Configured threshold in minutes for <see cref="StalePendingOrders"/>.</summary>
        public int StalePendingMinutes { get; set; }

        public decimal TotalRevenue { get; set; }

        public long TotalProductsSold { get; set; }

        public long TotalProducts { get; set; }

        public List<OrdersByStatusDto> OrdersByStatus { get; set; } = new List<OrdersByStatusDto>();

        public List<RevenueByDayDto> RevenueByDay { get; set; } = new List<RevenueByDayDto>();

        public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();

        /// <summary>
        /// Pre-orders (ScheduledFor set) not yet picked up/completed, ordered
        /// by scheduled time ascending — the kitchen prioritizes these.
        /// </summary>
        public List<OrderDto> UpcomingPreOrders { get; set; } = new List<OrderDto>();
    }

    public class OrdersByStatusDto
    {
        public string Status { get; set; }

        public long Count { get; set; }
    }

    public class RevenueByDayDto
    {
        public DateTime Date { get; set; }

        public decimal Revenue { get; set; }

        public long Orders { get; set; }
    }

    public class TopProductDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public long QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }

    /// <summary>Item 233 (Admin): một dòng báo cáo số đơn hủy theo lý do.</summary>
    public class CancelReasonReportDto
    {
        /// <summary>Lý do hủy (null/rỗng đã được gộp thành "Không rõ lý do").</summary>
        public string Reason { get; set; }

        /// <summary>Số đơn hủy với lý do này trong khoảng thời gian.</summary>
        public int Count { get; set; }
    }

    /// <summary>Item 234 (Admin): món có ≥3 đánh giá 1–2 sao trong 30 ngày qua.</summary>
    public class PoorlyRatedDishDto
    {
        /// <summary>Tên món (null/rỗng gộp thành "(không rõ món)").</summary>
        public string ProductName { get; set; }

        /// <summary>Số lượt đánh giá thấp (≤2 sao) của món.</summary>
        public int BadCount { get; set; }

        /// <summary>Điểm trung bình các lượt đánh giá thấp (làm tròn 1 số lẻ).</summary>
        public double AvgRating { get; set; }

        /// <summary>Tối đa 3 bình luận chê mẫu (không trùng) để xem lại chất lượng.</summary>
        public List<string> SampleComments { get; set; } = new List<string>();

        /// <summary>Thời điểm đánh giá thấp gần nhất.</summary>
        public DateTime LastBadAt { get; set; }
    }
}
