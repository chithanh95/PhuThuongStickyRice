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
    }
}
