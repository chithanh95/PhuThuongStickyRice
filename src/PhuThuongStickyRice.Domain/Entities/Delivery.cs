using PhuThuongStickyRice.Domain.Enum;
using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Vận chuyển
    /// </summary>
    public class Delivery : Entity<Guid>, IAggregateRoot
    {
        /// <summary>
        /// Khối lượng
        /// </summary>
        public double Amout { get; set; }

        /// <summary>
        /// Loại khối lượng
        /// </summary>
        public DeliveryStatus DeliveryStatus { get; set; }

        /// <summary>
        /// Sản phẩm yêu cầu vận chuyển
        /// </summary>
        public bool IsDelivery { get; set; }

        /// <summary>
        /// Sản phẩm
        /// </summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Refer
        /// </summary>
        public virtual Product Product { get; set; }
    }
}
