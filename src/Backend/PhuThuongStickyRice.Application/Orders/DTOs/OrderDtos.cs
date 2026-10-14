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

        public string Notes { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        /// <summary>"Delivery" or "Pickup".</summary>
        public string FulfillmentMethod { get; set; }

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

        [Required]
        [StringLength(500)]
        public string CustomerAddress { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        /// <summary>Payment method: "Cod" (default), "BankTransfer", "MoMo" or "Wallet".</summary>
        public string PaymentMethod { get; set; }

        /// <summary>Optional discount/promo code entered at checkout.</summary>
        [StringLength(50)]
        public string DiscountCode { get; set; }

        /// <summary>"Delivery" (default) or "Pickup". Pickup orders have no delivery fee and no address.</summary>
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
    }

    public class OrderReviewDto
    {
        public Guid Id { get; set; }

        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        /// <summary>Hidden reviews are not shown publicly.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Admin reply, shown publicly under the review.</summary>
        public string AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }
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
    }

    public class GetOrdersInput : PagedAndSortedResultRequestDto
    {
        public string SearchTerm { get; set; }

        public string Status { get; set; }

        /// <summary>Filter by assigned shipper.</summary>
        public Guid? ShipperId { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        [Required]
        public string Status { get; set; }
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
}
