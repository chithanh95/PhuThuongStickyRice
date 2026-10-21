using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Kết quả phân trang từ API: tổng số bản ghi + danh sách của trang hiện tại.</summary>
    /// <typeparam name="T">Kiểu item trong trang.</typeparam>
    public class PagedResult<T>
    {
        public long TotalCount { get; set; }

        public List<T> Items { get; set; } = new List<T>();
    }

    /// <summary>Sản phẩm hiển thị trên trang shop (tên, giá, ảnh, size, topping có thể chọn).</summary>
    public class ShopProductModel
    {
        public Guid Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        /// <summary>
        /// Item 256: giá khuyến mãi (VND). Null = không KM. Card hiện
        /// "giá gốc → giá KM" khi có giá trị và nhỏ hơn giá bán.
        /// </summary>
        public decimal? PromoPrice { get; set; }

        /// <summary>
        /// Item 256 (bug-hunt 2026-10-19): giá bán hiệu dụng đưa vào giỏ —
        /// giá KM nếu có và rẻ hơn giá đang xét, ngược lại giữ giá gốc.
        /// Helper dùng chung cho MỌI đường thêm vào giỏ (card /shop,
        /// quick-view, popup upsell, gợi ý ở /cart, trang chủ, chat) để giá
        /// trong giỏ luôn khớp giá KM niêm yết — trước đây chỉ card chính
        /// và quick-view dùng giá KM, các đường còn lại cho giá gốc vào giỏ
        /// khiến tổng giỏ/checkout cao hơn giá đã quảng cáo. Cùng luật với
        /// Product.ResolveEffectivePrice ở backend (server vẫn là nguồn
        /// quyết định cuối khi tạo đơn).
        /// </summary>
        /// <param name="product">Món hàng (có thể mang giá KM từ API).</param>
        /// <param name="basePrice">Giá đang xét (giá gốc hoặc giá size đã chọn).</param>
        /// <returns>Giá KM nếu hợp lệ và rẻ hơn, ngược lại giá gốc.</returns>
        public static decimal EffectivePrice(ShopProductModel product, decimal basePrice)
        {
            return product?.PromoPrice is decimal promo && promo > 0 && promo < basePrice
                ? promo
                : basePrice;
        }

        public string Currency { get; set; }

        /// <summary>Item 81: selling unit — "suất" (per portion) or "kg" (per kilogram).</summary>
        public string Unit { get; set; }

        /// <summary>
        /// Hậu tố đơn vị hiển thị giá: "/kg" cho món bán theo cân, "/suất" cho
        /// các món còn lại. Gom về đây từ Index.razor/Shop.razor (trước đây mỗi
        /// trang copy 1 method giống hệt nhau) — codequality-team 2026-10-20.
        /// </summary>
        /// <param name="product">Món hàng (null-safe: null được coi là "/suất").</param>
        /// <returns>"/kg" hoặc "/suất".</returns>
        public static string UnitSuffix(ShopProductModel product) =>
            string.Equals(product?.Unit, "kg", StringComparison.OrdinalIgnoreCase) ? "/kg" : "/suất";

        /// <summary>Item 146: manual display order set by drag &amp; drop on /products.</summary>
        public int SortOrder { get; set; }

        public string Status { get; set; }

        /// <summary>Comma-separated dietary tags: "chay", "man", "cay".</summary>
        public string Tags { get; set; }

        public string Category { get; set; }

        public string AllergenInfo { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        /// <summary>
        /// Item 164: daily sale time windows as JSON, e.g.
        /// [{"from":"06:00","to":"10:00"}]. Null = sold all day.
        /// </summary>
        public string AvailableHoursJson { get; set; }

        /// <summary>Item 165: max portions sold per day. Null = unlimited.</summary>
        public int? DailyLimit { get; set; }

        /// <summary>
        /// Item 166: photo gallery as a JSON string array of image URLs
        /// (first = primary). Null = placeholder SVG.
        /// </summary>
        public string ImageUrlsJson { get; set; }

        /// <summary>
        /// Item 165: portions still sellable today (filled from
        /// api/products/daily-remaining). Null = no daily limit.
        /// </summary>
        public int? RemainingToday { get; set; }

        public List<ProductSizeOptionModel> Sizes { get; set; } = new List<ProductSizeOptionModel>();

        public DateTimeOffset CreatedDateTime { get; set; }
    }

    /// <summary>Tùy chọn kích cỡ của 1 món (vd Nhỏ/Vừa/Lớn), mỗi size có giá riêng.</summary>
    public class ProductSizeOptionModel
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Item 165: per-product daily-limit status ("Còn N suất").
    /// </summary>
    public class ProductDailyRemainingModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public int DailyLimit { get; set; }

        public int SoldToday { get; set; }

        public int RemainingToday { get; set; }
    }

    /// <summary>Phần ăn thêm (topping) kèm giá phụ thu cho 1 món.</summary>
    public class ToppingModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public bool IsActive { get; set; }
    }

    /// <summary>1 món thành phần trong gói combo (id, tên, giá gốc, số lượng trong gói).</summary>
    public class ComboItemModel
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal ProductPrice { get; set; }

        public int Quantity { get; set; }
    }

    /// <summary>Gói nhiều món bán chung 1 giá — thêm vào giỏ như 1 dòng đơn.</summary>
    public class ComboModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        public List<ComboItemModel> Items { get; set; } = new List<ComboItemModel>();
    }

    /// <summary>
    /// Một dòng trong giỏ hàng phía client. Dòng mới được ghép theo line key
    /// (món + size + tập topping) nên cùng món nhưng khác cấu hình là các dòng
    /// riêng; tiền dòng = đơn giá (đã gồm size + topping) × số lượng.
    /// </summary>
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

        public string ImageUrl { get; set; }

        /// <summary>
        /// Per-item note from the customer ("ghi chú món"), e.g. "ít hành".
        /// Flows into the order line at checkout (wave 6a, item 24).
        /// </summary>
        public string Note { get; set; }
    }

    /// <summary>1 dòng hàng trong payload gửi API khi tạo đơn (món, size, topping, số lượng).</summary>
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

    /// <summary>Payload gửi /api/orders khi khách bấm đặt hàng (checkout).</summary>
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
        public string FulfillmentMethod { get; set; } = "Pickup";

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

        /// <summary>
        /// Mã OTP xác thực SĐT — chỉ gửi khi khách chưa đăng nhập mà dùng
        /// ví (PaymentMethod=Wallet) hoặc đổi điểm; server kiểm tra với
        /// purpose "checkout-spend" và từ chối tạo đơn nếu thiếu/sai mã.
        /// </summary>
        public string OtpCode { get; set; }

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

    /// <summary>Thông tin đơn vừa đặt thành công — hiển thị ở màn hình xác nhận đơn.</summary>
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
    /// <summary>Thông tin tài khoản ngân hàng của shop — dùng để dựng QR VietQR cho khách chuyển khoản.</summary>
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

    /// <summary>Kết quả tạo thanh toán MoMo: URL PayUrl để chuyển khách sang MoMo thanh toán.</summary>
    public class MoMoCreatePaymentResultModel
    {
        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public decimal Amount { get; set; }

        public string PayUrl { get; set; }

        public bool TestMode { get; set; }
    }

    /// <summary>Payload MoMo gọi ngược về server khi khách thanh toán xong (server xác thực chữ ký).</summary>
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

    /// <summary>1 dòng hàng hiển thị ở trang tra cứu đơn (chỉ xem, không chỉnh sửa).</summary>
    public class TrackedOrderLineModel
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }

        public string SizeName { get; set; }

        /// <summary>
        /// Id size đã chọn lúc đặt — "đặt lại" khôi phục đúng size theo id
        /// thay vì đoán theo tên. Null với đơn cũ chưa lưu id.
        /// </summary>
        public Guid? SizeOptionId { get; set; }

        public string ToppingNames { get; set; }

        /// <summary>
        /// Id các topping đã chọn lúc đặt — "đặt lại" khôi phục đúng topping
        /// theo id. Rỗng với đơn cũ chưa lưu id.
        /// </summary>
        public List<Guid> ToppingIds { get; set; } = new List<Guid>();

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

        /// <summary>Item 233: lý do hủy đơn (hiện cho khách xem đơn của mình).</summary>
        public string CancelReason { get; set; }

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

        public string PromotionName { get; set; }

        public decimal PromotionDiscountAmount { get; set; }

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
        /// <summary>Item 180: new-order webhook URL (báo đơn mới).</summary>
        public string NewOrderWebhookUrl { get; set; }

        /// <summary>Item 180: enable the new-order webhook.</summary>
        public bool NewOrderWebhookEnabled { get; set; }

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

        /// <summary>Item 101: show the "💬 Hỏi qua Zalo" button on each product card. Default true.</summary>
        public bool ShowZaloChatButton { get; set; } = true;

        /// <summary>Link tới Google Maps của quán (section "Đánh giá trên Google").</summary>
        public string GoogleMapsUrl { get; set; }

        /// <summary>Landing item 18: Facebook Pixel ID để đo chuyển đổi.</summary>
        public string FacebookPixelId { get; set; }

        /// <summary>Landing item 18: Google Analytics Measurement ID (G-...).</summary>
        public string GoogleAnalyticsId { get; set; }

        /// <summary>Item 53: Facebook Page Access Token for the Messenger chatbot.</summary>
        public string FacebookPageAccessToken { get; set; }

        /// <summary>Item 53: verify token for the Facebook webhook subscription.</summary>
        public string FacebookVerifyToken { get; set; }

        /// <summary>
        /// bug-hunt 2026-10-19: Facebook App Secret để verify chữ ký
        /// X-Hub-Signature-256 của webhook (chống giả mạo tạo đơn ảo).
        /// </summary>
        public string FacebookAppSecret { get; set; }

        /// <summary>Wave 6b (item 30): when false, checkout is blocked.</summary>
        public bool AcceptingOrders { get; set; } = true;

        public string PausedReason { get; set; }

        /// <summary>Item 117: true khi web đang nghỉ vì lịch nghỉ lễ hôm nay.</summary>
        public bool IsHolidayPaused { get; set; }

        /// <summary>
        /// Wave 6d (item 39): max non-cancelled orders per phone per day
        /// (Vietnam day). 0 = unlimited. Default 10.
        /// </summary>
        public int MaxOrdersPerPhonePerDay { get; set; } = 10;

        /// <summary>Item 138: Zalo OA access token for the Monday weekly report. Empty = show report in admin + copy button only.</summary>
        public string ZaloOaAccessToken { get; set; }

        /// <summary>Item 138: owner phone number.</summary>
        public string OwnerPhone { get; set; }

        /// <summary>Item 138: Zalo user id of the owner (OA messages are addressed by user id).</summary>
        public string OwnerZaloUserId { get; set; }
    }

    /// <summary>Result of GET api/dashboard/today-sold.</summary>
    /// <summary>Tổng số gói đã bán trong ngày hôm nay — dùng cho số liệu bán hàng nhanh.</summary>
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
    public class XoiCungReminderModel
    {
        public Guid Id { get; set; }

        public string CustomerName { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>"Ram" or "Mung1".</summary>
        public string ReminderType { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Public: last order name/address for a phone number.</summary>
    public class LastOrderInfoModel
    {
        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public DateTime LastOrderAt { get; set; }
    }

    /// <summary>Item 143: first-order voucher for a new customer.</summary>
    public class FirstOrderVoucherModel
    {
        public bool IsNewCustomer { get; set; }

        public string Code { get; set; }

        public int DiscountPercent { get; set; }
    }
}
