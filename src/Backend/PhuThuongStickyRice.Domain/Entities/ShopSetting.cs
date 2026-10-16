using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Shop commerce settings. Single-row table: the application service
    /// creates the default row on first read if none exists.
    /// Amounts are in VND.
    /// </summary>
    public class ShopSetting : Entity<Guid>, IAggregateRoot
    {
        /// <summary>Flat delivery fee applied when the order is below <see cref="FreeShipThreshold"/>.</summary>
        public decimal DeliveryFee { get; set; }

        /// <summary>Orders with (subtotal - discount) at or above this are free shipping. 0 disables the fee entirely.</summary>
        public decimal FreeShipThreshold { get; set; }

        /// <summary>Minimum order subtotal. 0 = no minimum.</summary>
        public decimal MinOrderAmount { get; set; }

        /// <summary>
        /// Estimated delivery time in minutes, shown at checkout
        /// ("Dự kiến giao trong 30–45 phút"). Default 45.
        /// </summary>
        public int EstimatedDeliveryMinutes { get; set; }

        /// <summary>
        /// Unpaid bank-transfer orders older than this (minutes) are
        /// auto-cancelled by the background worker. Default 30.
        /// </summary>
        public int BankTransferExpiryMinutes { get; set; }

        /// <summary>Loyalty: VND spent per 1 earned point. Default 10000.</summary>
        public decimal PointsPerVnd { get; set; }

        /// <summary>Loyalty: points needed for one redemption step. Default 100.</summary>
        public int PointsRedeemUnit { get; set; }

        /// <summary>Loyalty: VND off per redemption step. Default 10000.</summary>
        public decimal PointsRedeemValue { get; set; }

        /// <summary>Loyalty points awarded to both referrer and buyer on the
        /// buyer's first completed order with a referral code. Default 50.</summary>
        public int ReferralBonusPoints { get; set; }

        /// <summary>Orders at or above this total on bank transfer require a
        /// deposit. Default 500000.</summary>
        public decimal DepositThreshold { get; set; }

        /// <summary>Deposit percent of the order total. Default 30.</summary>
        public decimal DepositPercent { get; set; }

        /// <summary>Gift packaging fee in VND. Default 20000.</summary>
        public decimal GiftFee { get; set; }

        /// <summary>Lifetime spend (VND) for the Bạc (Silver) tier. Default 1000000.</summary>
        public decimal TierSilverMin { get; set; }

        /// <summary>Silver tier discount percent. Default 2.</summary>
        public decimal TierSilverDiscount { get; set; }

        /// <summary>Lifetime spend (VND) for the Vàng (Gold) tier. Default 5000000.</summary>
        public decimal TierGoldMin { get; set; }

        /// <summary>Gold tier discount percent. Default 5.</summary>
        public decimal TierGoldDiscount { get; set; }

        /// <summary>
        /// Owner email for the weekly/monthly auto report. Empty = disabled.
        /// </summary>
        public string OwnerReportEmail { get; set; }

        /// <summary>
        /// Orders still Pending after this many minutes trigger the urgent
        /// unconfirmed-order alert on the dashboard. Default 15.
        /// </summary>
        public int UnconfirmedAlertMinutes { get; set; }

        /// <summary>reCAPTCHA v3 site key. Empty = reCAPTCHA disabled.</summary>
        public string RecaptchaSiteKey { get; set; }

        /// <summary>reCAPTCHA v3 secret key. Empty = reCAPTCHA disabled.</summary>
        public string RecaptchaSecretKey { get; set; }

        /// <summary>
        /// Public base URL of the shop web (e.g. https://xoiphuthuong.vn),
        /// used to build table QR codes. No trailing slash.
        /// </summary>
        public string WebBaseUrl { get; set; }

        /// <summary>
        /// Maintenance mode (bảo trì). When true, the public WASM app shows
        /// a friendly maintenance page; /login, /admin/* and /bep still work.
        /// </summary>
        public bool MaintenanceMode { get; set; }

        /// <summary>Message shown on the maintenance page.</summary>
        public string MaintenanceMessage { get; set; }

        /// <summary>
        /// Telegram bot token for health alerts. Empty = Telegram disabled.
        /// Secrets stay server-side; this is admin-configurable on purpose.
        /// </summary>
        public string TelegramBotToken { get; set; }

        /// <summary>Telegram chat id receiving health alerts. Empty = disabled.</summary>
        public string TelegramChatId { get; set; }

        /// <summary>When false, the SystemHealth worker only logs. Default true.</summary>
        public bool HealthAlertsEnabled { get; set; }

        /// <summary>
        /// Error entries in the last hour above this count trigger a health
        /// alert. Default 20.
        /// </summary>
        public int ErrorRateThreshold { get; set; }

        /// <summary>
        /// Uploads folder size (MB) above which /admin/he-thong shows a
        /// warning banner. Default 500.
        /// </summary>
        public int UploadsSizeWarningMb { get; set; }

        // ---- Payment config (item 106): non-secret display values, admin-editable
        // at /admin/thanh-toan. Secrets stay in appsettings/Production.json. ----

        /// <summary>Bank transfer: bank display name, e.g. "Vietcombank".</summary>
        public string BankName { get; set; }

        /// <summary>Bank transfer: VietQR bank code, e.g. "VCB".</summary>
        public string BankId { get; set; }

        /// <summary>Bank transfer: account number.</summary>
        public string BankAccountNumber { get; set; }

        /// <summary>Bank transfer: account holder name.</summary>
        public string BankAccountName { get; set; }

        /// <summary>Bank transfer: VietQR image template (compact/compact2/qr_only).</summary>
        public string BankQrTemplate { get; set; }

        /// <summary>MoMo: partner code (non-secret). AccessKey/SecretKey stay in appsettings.</summary>
        public string MoMoPartnerCode { get; set; }

        /// <summary>MoMo: create-payment endpoint URL override. Empty = appsettings.</summary>
        public string MoMoEndpoint { get; set; }

        // ---- Cloud image backup (item 114) ----

        /// <summary>
        /// Image backup provider name, e.g. "None" (default) or "S3".
        /// Only "None" is implemented; see docs for adding a provider.
        /// </summary>
        public string ImageBackupProvider { get; set; }

        /// <summary>JSON settings for the image backup provider (e.g. connection string).</summary>
        public string ImageBackupSettingsJson { get; set; }

        // ---- Wave 6a (item 22): open-hours display ----

        /// <summary>
        /// Daily opening time ("HH:mm", Vietnam local time). Default "06:00".
        /// Shown on the shop hero and landing page (item 22). The admin
        /// config UI and checkout gating arrive in Wave 6b.
        /// </summary>
        public string OpenTime { get; set; }

        /// <summary>Daily closing time ("HH:mm", Vietnam local time). Default "10:30".</summary>
        public string CloseTime { get; set; }

        // ---- Wave 6b (items 27, 30) ----

        /// <summary>
        /// Hotline shown on the floating mobile call button (item 27).
        /// Empty = fall back to the landing page ContactPhone.
        /// </summary>
        public string Hotline { get; set; }

        /// <summary>
        /// Phone used for the floating mobile Zalo chat button (item 27).
        /// Empty = fall back to the landing page ContactPhone.
        /// </summary>
        public string ZaloPhone { get; set; }

        /// <summary>
        /// Item 101: when true, each product card on /shop and the quick-view
        /// modal show a "💬 Hỏi qua Zalo" button linking to
        /// zalo.me/&lt;ZaloPhone or Hotline&gt; with a pre-filled message.
        /// Default true.
        /// </summary>
        public bool ShowZaloChatButton { get; set; }

        /// <summary>
        /// Link tới Google Maps của quán (đánh giá trên Google).
        /// Admin nhập 1 lần ở Cài đặt cửa hàng.
        /// </summary>
        public string GoogleMapsUrl { get; set; }

        /// <summary>
        /// Facebook Page Access Token for the Messenger chatbot (item 53).
        /// Empty = chatbot runs in "waiting for configuration" mode (webhook
        /// still verifies, but no replies are sent). Stored data-protected.
        /// </summary>
        public string FacebookPageAccessToken { get; set; }

        /// <summary>
        /// Verify token used when subscribing the Facebook webhook
        /// (must match the token entered in the Facebook App dashboard).
        /// </summary>
        public string FacebookVerifyToken { get; set; }

        /// <summary>
        /// Wave 6b (item 30): when false, checkout is blocked with a
        /// friendly message, even inside open hours. Default true.
        /// </summary>
        public bool AcceptingOrders { get; set; }

        /// <summary>
        /// Reason shown to customers when <see cref="AcceptingOrders"/> is
        /// false, e.g. "Hết nếp, mở lại lúc 14:00". Null/empty = generic message.
        /// </summary>
        public string PausedReason { get; set; }

        /// <summary>
        /// Wave 6d (item 39): max non-cancelled orders per phone per day
        /// (Vietnam day). 0 = unlimited. Default 10.
        /// </summary>
        public int MaxOrdersPerPhonePerDay { get; set; }

        // ---- Wave 5 (items 121/124/139) ----

        /// <summary>Loyalty points awarded per daily check-in. Default 5.</summary>
        public int CheckInPoints { get; set; }

        /// <summary>
        /// Grams of nếp per sold portion, used by the ingredient forecast
        /// heuristic (item 124). Default 150.
        /// </summary>
        public int NepPerPortionGrams { get; set; }

        /// <summary>
        /// Daily revenue target in VND shown on the dashboard (item 139).
        /// Default 2000000.
        /// </summary>
        public decimal DailyTargetAmount { get; set; }

        // ---- Item 138: weekly owner report via Zalo OA ----

        /// <summary>
        /// Zalo OA access token used to send the Monday-morning weekly
        /// report to the owner. Empty = report is shown in admin with a
        /// copy button only (no pretend-send). Stored data-protected.
        /// </summary>
        public string ZaloOaAccessToken { get; set; }

        /// <summary>Owner phone number (displayed in the admin UI, used as
        /// a fallback recipient identifier).</summary>
        public string OwnerPhone { get; set; }

        /// <summary>
        /// Zalo user id of the shop owner. Zalo OA messages are addressed
        /// by user id, not phone number — the owner must follow/interact
        /// with the OA once for the id to be obtainable.
        /// </summary>
        public string OwnerZaloUserId { get; set; }

        /// <summary>
        /// Idempotency marker for the Monday worker ("yyyy-MM-dd" of the
        /// reported week's Monday). Prevents double-sends after restarts.
        /// </summary>
        public string LastWeeklyReportSentWeek { get; set; }

        // ---- Item 180: simple "new order" webhook (báo đơn mới) ----

        /// <summary>
        /// Item 180: URL nhận thông báo khi có đơn mới. POST JSON
        /// {maDon, sdt, tongTien, trangThai, thoiGian}. Empty = disabled.
        /// (Hệ thống WebhookEndpoint generic của item 110 vẫn hoạt động song
        /// song — đây là chế độ đơn giản 1 URL cho chủ quán không rành kỹ thuật.)
        /// </summary>
        public string NewOrderWebhookUrl { get; set; }

        /// <summary>Item 180: bật/tắt webhook báo đơn mới.</summary>
        public bool NewOrderWebhookEnabled { get; set; }

        // ---- Landing conversion tracking (admin nhập ID, không hardcode) ----

        /// <summary>
        /// Facebook Pixel ID (dạng số, ví dụ "123456789012345").
        /// Empty = không gắn pixel. Dùng để đo chuyển đổi landing → đặt hàng.
        /// </summary>
        public string FacebookPixelId { get; set; }

        /// <summary>
        /// Google Analytics 4 Measurement ID (dạng "G-XXXXXXXXXX").
        /// Empty = không gắn GA.
        /// </summary>
        public string GoogleAnalyticsId { get; set; }

        public static ShopSetting CreateDefault()
        {
            return new ShopSetting
            {
                DeliveryFee = 20000,
                FreeShipThreshold = 200000,
                MinOrderAmount = 0,
                EstimatedDeliveryMinutes = 45,
                BankTransferExpiryMinutes = 30,
                PointsPerVnd = 10000,
                PointsRedeemUnit = 100,
                PointsRedeemValue = 10000,
                ReferralBonusPoints = 50,
                DepositThreshold = 500000,
                DepositPercent = 30,
                GiftFee = 20000,
                TierSilverMin = 1000000,
                TierSilverDiscount = 2,
                TierGoldMin = 5000000,
                TierGoldDiscount = 5,
                OwnerReportEmail = null,
                UnconfirmedAlertMinutes = 15,
                RecaptchaSiteKey = null,
                RecaptchaSecretKey = null,
                WebBaseUrl = null,
                MaintenanceMode = false,
                MaintenanceMessage = "Cửa hàng đang bảo trì, vui lòng quay lại sau.",
                TelegramBotToken = null,
                TelegramChatId = null,
                HealthAlertsEnabled = true,
                ErrorRateThreshold = 20,
                UploadsSizeWarningMb = 500,
                BankName = null,
                BankId = null,
                BankAccountNumber = null,
                BankAccountName = null,
                BankQrTemplate = null,
                MoMoPartnerCode = null,
                MoMoEndpoint = null,
                ImageBackupProvider = "None",
                ImageBackupSettingsJson = null,
                CheckInPoints = 5,
                NepPerPortionGrams = 150,
                DailyTargetAmount = 2000000,
                OpenTime = "06:00",
                CloseTime = "10:30",
                Hotline = null,
                ZaloPhone = null,
                ShowZaloChatButton = true,
                GoogleMapsUrl = null,
                AcceptingOrders = true,
                MaxOrdersPerPhonePerDay = 10,
                NewOrderWebhookUrl = null,
                NewOrderWebhookEnabled = false,
                FacebookPixelId = null,
                GoogleAnalyticsId = null
            };
        }
    }
}
