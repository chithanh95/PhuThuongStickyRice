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

        public static ShopSetting CreateDefault()
        {
            return new ShopSetting
            {
                DeliveryFee = 20000,
                FreeShipThreshold = 200000,
                MinOrderAmount = 0,
                EstimatedDeliveryMinutes = 45
            };
        }
    }
}
