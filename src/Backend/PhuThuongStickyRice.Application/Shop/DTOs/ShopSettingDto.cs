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
    }
}
