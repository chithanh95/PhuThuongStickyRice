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
                TierGoldDiscount = 5
            };
        }
    }
}
