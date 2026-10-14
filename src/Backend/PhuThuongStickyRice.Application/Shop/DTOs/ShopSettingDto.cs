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
    }
}
