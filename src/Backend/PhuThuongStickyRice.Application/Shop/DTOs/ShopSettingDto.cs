using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Application.Shop.DTOs
{
    /// <summary>Shop commerce settings. Amounts in VND.</summary>
    public class ShopSettingDto
    {
        [Range(0, double.MaxValue)]
        public decimal DeliveryFee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal FreeShipThreshold { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; }

        /// <summary>Estimated delivery time in minutes, shown at checkout.</summary>
        [Range(0, 600)]
        public int EstimatedDeliveryMinutes { get; set; }

        /// <summary>Unpaid bank-transfer orders auto-cancel after X minutes.</summary>
        [Range(1, 10080)]
        public int BankTransferExpiryMinutes { get; set; }

        /// <summary>Loyalty: VND spent per 1 earned point.</summary>
        [Range(1, double.MaxValue)]
        public decimal PointsPerVnd { get; set; }

        /// <summary>Loyalty: points needed for one redemption step.</summary>
        [Range(1, int.MaxValue)]
        public int PointsRedeemUnit { get; set; }

        /// <summary>Loyalty: VND off per redemption step.</summary>
        [Range(0, double.MaxValue)]
        public decimal PointsRedeemValue { get; set; }

        /// <summary>Loyalty points for both referrer and buyer on first completed referral order.</summary>
        [Range(0, int.MaxValue)]
        public int ReferralBonusPoints { get; set; }

        /// <summary>Bank-transfer orders at/above this total require a deposit.</summary>
        [Range(0, double.MaxValue)]
        public decimal DepositThreshold { get; set; }

        /// <summary>Deposit percent of the order total.</summary>
        [Range(0, 100)]
        public decimal DepositPercent { get; set; }

        /// <summary>Gift packaging fee.</summary>
        [Range(0, double.MaxValue)]
        public decimal GiftFee { get; set; }

        /// <summary>Lifetime spend for the Bạc (Silver) tier.</summary>
        [Range(0, double.MaxValue)]
        public decimal TierSilverMin { get; set; }

        /// <summary>Silver tier discount percent.</summary>
        [Range(0, 100)]
        public decimal TierSilverDiscount { get; set; }

        /// <summary>Lifetime spend for the Vàng (Gold) tier.</summary>
        [Range(0, double.MaxValue)]
        public decimal TierGoldMin { get; set; }

        /// <summary>Gold tier discount percent.</summary>
        [Range(0, 100)]
        public decimal TierGoldDiscount { get; set; }

        /// <summary>
        /// Owner email for the weekly/monthly auto report. Empty = disabled.
        /// </summary>
        [StringLength(254)]
        [EmailAddress]
        public string OwnerReportEmail { get; set; }

        /// <summary>
        /// Pending orders older than this (minutes) trigger the urgent
        /// unconfirmed-order dashboard alert.
        /// </summary>
        [Range(1, 10080)]
        public int UnconfirmedAlertMinutes { get; set; }

        /// <summary>reCAPTCHA v3 site key. Empty = reCAPTCHA disabled.</summary>
        [StringLength(100)]
        public string RecaptchaSiteKey { get; set; }

        /// <summary>reCAPTCHA v3 secret key. Empty = reCAPTCHA disabled.</summary>
        [StringLength(100)]
        public string RecaptchaSecretKey { get; set; }

        /// <summary>Public base URL of the shop web, used for table QR codes.</summary>
        [StringLength(500)]
        public string WebBaseUrl { get; set; }

        /// <summary>Maintenance mode (bảo trì): public pages show a friendly page.</summary>
        public bool MaintenanceMode { get; set; }

        /// <summary>Message shown on the maintenance page.</summary>
        [StringLength(500)]
        public string MaintenanceMessage { get; set; }

        /// <summary>Telegram bot token for health alerts. Empty = disabled.</summary>
        [StringLength(100)]
        public string TelegramBotToken { get; set; }

        /// <summary>Telegram chat id receiving health alerts. Empty = disabled.</summary>
        [StringLength(50)]
        public string TelegramChatId { get; set; }

        /// <summary>When false, the health worker only logs.</summary>
        public bool HealthAlertsEnabled { get; set; }

        /// <summary>Error entries in the last hour above this count trigger an alert.</summary>
        [Range(1, int.MaxValue)]
        public int ErrorRateThreshold { get; set; }

        /// <summary>Uploads folder size (MB) above which /admin/he-thong warns.</summary>
        [Range(1, int.MaxValue)]
        public int UploadsSizeWarningMb { get; set; }

        // ---- Payment config (item 106): non-secret display values ----

        /// <summary>Bank transfer: bank display name.</summary>
        [StringLength(200)]
        public string BankName { get; set; }

        /// <summary>Bank transfer: VietQR bank code, e.g. "VCB".</summary>
        [StringLength(20)]
        public string BankId { get; set; }

        /// <summary>Bank transfer: account number.</summary>
        [StringLength(50)]
        public string BankAccountNumber { get; set; }

        /// <summary>Bank transfer: account holder name.</summary>
        [StringLength(200)]
        public string BankAccountName { get; set; }

        /// <summary>Bank transfer: VietQR image template (compact/compact2/qr_only).</summary>
        [StringLength(20)]
        public string BankQrTemplate { get; set; }

        /// <summary>MoMo: partner code (non-secret).</summary>
        [StringLength(50)]
        public string MoMoPartnerCode { get; set; }

        /// <summary>MoMo: create-payment endpoint URL override. Empty = appsettings.</summary>
        [StringLength(500)]
        public string MoMoEndpoint { get; set; }

        // ---- Cloud image backup (item 114) ----

        /// <summary>Image backup provider name ("None" default).</summary>
        [StringLength(50)]
        public string ImageBackupProvider { get; set; }

        /// <summary>JSON settings for the image backup provider.</summary>
        [StringLength(4000)]
        public string ImageBackupSettingsJson { get; set; }

        // ---- Wave 5 (items 121/124/139) ----

        /// <summary>Loyalty points per daily check-in. Default 5.</summary>
        [Range(0, 1000)]
        public int CheckInPoints { get; set; }

        /// <summary>Grams of nếp per portion (forecast heuristic). Default 150.</summary>
        [Range(1, 1000)]
        public int NepPerPortionGrams { get; set; }

        /// <summary>Daily revenue target in VND (dashboard). Default 2000000.</summary>
        [Range(0, 1000000000)]
        public decimal DailyTargetAmount { get; set; }

        // ---- Wave 6a (item 22): open-hours display ("HH:mm") ----

        /// <summary>Daily opening time ("HH:mm", Vietnam local time). Default "06:00".</summary>
        [StringLength(5)]
        public string OpenTime { get; set; }

        /// <summary>Daily closing time ("HH:mm", Vietnam local time). Default "10:30".</summary>
        [StringLength(5)]
        public string CloseTime { get; set; }

        // ---- Wave 6b (items 27, 30) ----

        /// <summary>Hotline for the floating mobile call button. Empty = landing ContactPhone.</summary>
        [StringLength(30)]
        public string Hotline { get; set; }

        /// <summary>Phone for the floating mobile Zalo button. Empty = landing ContactPhone.</summary>
        [StringLength(30)]
        public string ZaloPhone { get; set; }

        /// <summary>
        /// Facebook Page Access Token for the Messenger chatbot. Empty = chatbot
        /// disabled (webhook verifies but no replies are sent).
        /// </summary>
        [StringLength(500)]
        public string FacebookPageAccessToken { get; set; }

        /// <summary>Verify token for the Facebook webhook subscription.</summary>
        [StringLength(200)]
        public string FacebookVerifyToken { get; set; }

        /// <summary>When false, checkout is blocked. Default true.</summary>
        public bool AcceptingOrders { get; set; } = true;

        /// <summary>Reason shown to customers while orders are paused.</summary>
        [StringLength(500)]
        public string PausedReason { get; set; }

        // ---- Wave 6d (item 39): anti-spam ----

        /// <summary>
        /// Max non-cancelled orders per phone per day (Vietnam day).
        /// 0 = unlimited. Default 10.
        /// </summary>
        [Range(0, 1000)]
        public int MaxOrdersPerPhonePerDay { get; set; } = 10;
    }
}
