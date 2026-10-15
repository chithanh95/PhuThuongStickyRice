using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    public class PagedResult<T>
    {
        public long TotalCount { get; set; }

        public List<T> Items { get; set; } = new List<T>();
    }

    public class ShopProductModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }

        /// <summary>Comma-separated dietary tags: "chay", "man", "cay".</summary>
        public string Tags { get; set; }

        public string Category { get; set; }

        public string AllergenInfo { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        public List<ProductSizeOptionModel> Sizes { get; set; } = new List<ProductSizeOptionModel>();

        public DateTimeOffset CreatedDateTime { get; set; }
    }

    public class ProductSizeOptionModel
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    public class ToppingModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public bool IsActive { get; set; }
    }

    public class ComboItemModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }
    }

    public class ComboModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public List<ComboItemModel> Items { get; set; } = new List<ComboItemModel>();
    }

    public class CartItemModel
    {
        /// <summary>
        /// Stable key for the configured line: product/combo id + size +
        /// toppings, so different configurations are separate lines.
        /// </summary>
        public string LineKey { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductCode { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal => UnitPrice * Quantity;

        public Guid? SizeOptionId { get; set; }

        public string SizeName { get; set; }

        public List<Guid> ToppingIds { get; set; } = new List<Guid>();

        public string ToppingNames { get; set; }

        public decimal ToppingsTotal { get; set; }

        public bool IsCombo { get; set; }

        /// <summary>
        /// Per-item note from the customer ("ghi chú món"), e.g. "ít hành".
        /// Flows into the order line at checkout (wave 6a, item 24).
        /// </summary>
        public string Note { get; set; }
    }

    public class CreateOrderLineModel
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public Guid? SizeOptionId { get; set; }

        public List<Guid> ToppingIds { get; set; } = new List<Guid>();

        public bool IsCombo { get; set; }

        /// <summary>Optional per-item note (rendered on sticky labels).</summary>
        public string Note { get; set; }
    }

    public class CreateOrderModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Optional. When provided, the order confirmation email is sent here.</summary>
        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        /// <summary>"Cod" (default), "BankTransfer", "MoMo" or "Wallet".</summary>
        public string PaymentMethod { get; set; } = "Cod";

        /// <summary>Item 133: "Web" / "QR bàn" / "Link chia sẻ" / "Nhóm".</summary>
        public string Source { get; set; }

        /// <summary>Optional discount/promo code.</summary>
        public string DiscountCode { get; set; }

        /// <summary>"ShipperPickup" (default), "Pickup" or legacy "Delivery".</summary>
        public string FulfillmentMethod { get; set; } = "ShipperPickup";

        /// <summary>
        /// Name of the customer-booked shipper (Grab/Ahamove/...) for a
        /// ShipperPickup order. Optional.
        /// </summary>
        public string PickupShipperName { get; set; }

        /// <summary>
        /// Phone of the customer-booked shipper for a ShipperPickup order.
        /// Optional.
        /// </summary>
        public string PickupShipperPhone { get; set; }

        /// <summary>Pre-order scheduled pickup time (UTC). Null = ASAP.</summary>
        public DateTime? ScheduledFor { get; set; }

        /// <summary>Optional VAT invoice tax code.</summary>
        public string TaxCode { get; set; }

        /// <summary>Optional VAT invoice company name.</summary>
        public string CompanyName { get; set; }

        /// <summary>Recipient name when ordering for someone else.</summary>
        public string RecipientName { get; set; }

        /// <summary>Recipient phone when ordering for someone else.</summary>
        public string RecipientPhone { get; set; }

        /// <summary>Gift packaging requested.</summary>
        public bool IsGift { get; set; }

        public string GiftMessage { get; set; }

        /// <summary>Loyalty points the customer wants to redeem.</summary>
        public int RedeemPoints { get; set; }

        /// <summary>Referrer's referral code, if any.</summary>
        public string ReferralCode { get; set; }

        /// <summary>Optional birthday (updates the loyalty profile).</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Idempotency key for the checkout attempt.</summary>
        public string IdempotencyKey { get; set; }

        /// <summary>Item 109: optional branch id.</summary>
        public Guid? BranchId { get; set; }

        /// <summary>reCAPTCHA v3 token (verified when reCAPTCHA is enabled).</summary>
        public string RecaptchaToken { get; set; }

        /// <summary>
        /// Wave 6a (item 23): scheduled delivery slot ("06:00–06:30").
        /// Null = giao ngay.
        /// </summary>
        public string DeliverySlot { get; set; }

        public List<CreateOrderLineModel> Lines { get; set; } = new List<CreateOrderLineModel>();
    }

    public class PlacedOrderModel
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public string DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal LoyaltyDiscount { get; set; }

        public int LoyaltyPointsRedeemed { get; set; }

        public decimal TierDiscount { get; set; }

        public string TaxCode { get; set; }

        public string CompanyName { get; set; }

        public bool RequiresDeposit { get; set; }

        public string RecipientName { get; set; }

        public string RecipientPhone { get; set; }

        public bool IsGift { get; set; }

        public string GiftMessage { get; set; }

        public decimal GiftFee { get; set; }

        public bool LuckySpinUsed { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        /// <summary>Item 91: happy-hour promotion auto-applied at checkout.</summary>
        public string PromotionName { get; set; }

        public decimal PromotionDiscountAmount { get; set; }

        public string FulfillmentMethod { get; set; }

        /// <summary>Name of the customer-booked pickup shipper. Nullable.</summary>
        public string PickupShipperName { get; set; }

        /// <summary>Phone of the customer-booked pickup shipper. Nullable.</summary>
        public string PickupShipperPhone { get; set; }

        /// <summary>When the order was collected at the shop (UTC). Nullable.</summary>
        public DateTime? PickedUpAt { get; set; }

        /// <summary>Pre-order scheduled pickup time (UTC). Null = ASAP.</summary>
        public DateTime? ScheduledFor { get; set; }

        /// <summary>Wave 6a (item 23): scheduled delivery slot. Null = giao ngay.</summary>
        public string DeliverySlot { get; set; }
    }

    /// <summary>Public bank-transfer details shown at checkout (account info + VietQR).</summary>
    public class BankTransferInfoModel
    {
        public string BankName { get; set; }

        public string BankId { get; set; }

        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public string QrTemplate { get; set; }

        /// <summary>True when the shop has configured a real bank account (no 'xxx' placeholders).</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BankId) && BankId != "xxx"
            && !string.IsNullOrWhiteSpace(AccountNumber) && AccountNumber != "xxx";
    }

    public class MoMoCreatePaymentResultModel
    {
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public decimal Amount { get; set; }

        public string PayUrl { get; set; }

        public bool TestMode { get; set; }
    }

    public class MoMoCallbackModel
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

        public string SimulatedSignature { get; set; }

        public bool Simulated { get; set; }
    }

    public class TrackedOrderLineModel
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

        /// <summary>Per-item note from the customer (wave 6a, item 24).</summary>
        public string Note { get; set; }
    }

    /// <summary>Order as returned by the public /api/orders/track endpoint.</summary>
    public class TrackedOrderModel
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public string CustomerEmail { get; set; }

        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        public string Status { get; set; }

        public string PaymentMethod { get; set; }

        public string PaymentStatus { get; set; }

        public string FulfillmentMethod { get; set; }

        /// <summary>Name of the customer-booked pickup shipper. Nullable.</summary>
        public string PickupShipperName { get; set; }

        /// <summary>Phone of the customer-booked pickup shipper. Nullable.</summary>
        public string PickupShipperPhone { get; set; }

        /// <summary>When the order was collected at the shop (UTC). Nullable.</summary>
        public DateTime? PickedUpAt { get; set; }

        /// <summary>Pre-order scheduled pickup time (UTC). Null = ASAP.</summary>
        public DateTime? ScheduledFor { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public string DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal LoyaltyDiscount { get; set; }

        public int LoyaltyPointsRedeemed { get; set; }

        public decimal TierDiscount { get; set; }

        public string ReferralCodeUsed { get; set; }

        public string TaxCode { get; set; }

        public string CompanyName { get; set; }

        public bool RequiresDeposit { get; set; }

        public string RecipientName { get; set; }

        public string RecipientPhone { get; set; }

        public bool IsGift { get; set; }

        public string GiftMessage { get; set; }

        public decimal GiftFee { get; set; }

        public bool LuckySpinUsed { get; set; }

        /// <summary>Item 130: "None" / "Requested" / "Refunded".</summary>
        public string RefundStatus { get; set; }

        /// <summary>Item 130: refund amount in VND.</summary>
        public decimal RefundAmount { get; set; }

        /// <summary>Wave 6a (item 23): scheduled delivery slot. Null = giao ngay.</summary>
        public string DeliverySlot { get; set; }

        public List<TrackedOrderLineModel> Lines { get; set; } = new List<TrackedOrderLineModel>();
    }

    /// <summary>Shop commerce settings. Amounts in VND.</summary>
    public class ShopSettingModel
    {
        public decimal DeliveryFee { get; set; }

        public decimal FreeShipThreshold { get; set; }

        public decimal MinOrderAmount { get; set; }

        public int EstimatedDeliveryMinutes { get; set; }

        public int BankTransferExpiryMinutes { get; set; }

        public decimal PointsPerVnd { get; set; }

        public int PointsRedeemUnit { get; set; }

        public decimal PointsRedeemValue { get; set; }

        public int ReferralBonusPoints { get; set; }

        public decimal DepositThreshold { get; set; }

        public decimal DepositPercent { get; set; }

        public decimal GiftFee { get; set; }

        public decimal TierSilverMin { get; set; }

        public decimal TierSilverDiscount { get; set; }

        public decimal TierGoldMin { get; set; }

        public decimal TierGoldDiscount { get; set; }

        public string OwnerReportEmail { get; set; }

        public int UnconfirmedAlertMinutes { get; set; }

        public string RecaptchaSiteKey { get; set; }

        public string RecaptchaSecretKey { get; set; }

        public string WebBaseUrl { get; set; }

        public bool MaintenanceMode { get; set; }

        public string MaintenanceMessage { get; set; }

        public string TelegramBotToken { get; set; }

        public string TelegramChatId { get; set; }

        public bool HealthAlertsEnabled { get; set; }

        public int ErrorRateThreshold { get; set; }

        public int UploadsSizeWarningMb { get; set; }

        public string BankName { get; set; }

        public string BankId { get; set; }

        public string BankAccountNumber { get; set; }

        public string BankAccountName { get; set; }

        public string BankQrTemplate { get; set; }

        public string MoMoPartnerCode { get; set; }

        public string MoMoEndpoint { get; set; }

        public string ImageBackupProvider { get; set; }

        public string ImageBackupSettingsJson { get; set; }

        /// <summary>Item 121: loyalty points per daily check-in.</summary>
        public int CheckInPoints { get; set; }

        /// <summary>Item 124: grams of nếp per portion (forecast).</summary>
        public int NepPerPortionGrams { get; set; }

        /// <summary>Item 139: daily revenue target in VND.</summary>
        public decimal DailyTargetAmount { get; set; }

        /// <summary>Wave 6a (item 22): daily opening time "HH:mm" (VN time). Default "06:00".</summary>
        public string OpenTime { get; set; }

        /// <summary>Wave 6a (item 22): daily closing time "HH:mm" (VN time). Default "10:30".</summary>
        public string CloseTime { get; set; }

        /// <summary>Wave 6b (item 27): hotline for the floating mobile call button.</summary>
        public string Hotline { get; set; }

        /// <summary>Wave 6b (item 27): phone for the floating mobile Zalo button.</summary>
        public string ZaloPhone { get; set; }

        /// <summary>Link tới Google Maps của quán (section "Đánh giá trên Google").</summary>
        public string GoogleMapsUrl { get; set; }

        /// <summary>Item 53: Facebook Page Access Token for the Messenger chatbot.</summary>
        public string FacebookPageAccessToken { get; set; }

        /// <summary>Item 53: verify token for the Facebook webhook subscription.</summary>
        public string FacebookVerifyToken { get; set; }

        /// <summary>Wave 6b (item 30): when false, checkout is blocked.</summary>
        public bool AcceptingOrders { get; set; } = true;

        public string PausedReason { get; set; }

        /// <summary>
        /// Wave 6d (item 39): max non-cancelled orders per phone per day
        /// (Vietnam day). 0 = unlimited. Default 10.
        /// </summary>
        public int MaxOrdersPerPhonePerDay { get; set; } = 10;
    }

    /// <summary>Result of GET api/dashboard/today-sold.</summary>
    public class TodaySoldModel
    {
        public long QuantitySold { get; set; }
    }

    /// <summary>Bulk/event quote request (đặt tiệc).</summary>
    public class QuoteRequestModel
    {
        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        public DateTime? EventDate { get; set; }

        public int Quantity { get; set; }

        public string Notes { get; set; }

        public string EventLocation { get; set; }

        public string Budget { get; set; }

        public string QuoteType { get; set; }
    }

    /// <summary>Monthly xoi-cung reminder subscription (Rằm / mùng 1).</summary>
    public class CungReminderModel
    {
        public Guid Id { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>"Ram" or "Mung1".</summary>
        public string ReminderType { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class DishCandidateModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public long Votes { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>Public: last order name/address for a phone number.</summary>
    public class LastOrderInfoModel
    {
        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public DateTime LastOrderAt { get; set; }
    }
}
