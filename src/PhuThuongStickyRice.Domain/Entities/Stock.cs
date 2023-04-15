using PhuThuongStickyRice.Domain.Enum;
using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Kho hàng
    /// </summary>
    public class Stock : Entity<Guid>, IAggregateRoot
    {
        /// <summary>
        /// Mã sản phẩm / SKU
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Mã vạch / Barcode (ISBN, UPC, v.v...)
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// Số lượng
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        /// Cho phép tiếp tục đặt hàng khi hết hàng
        /// </summary>
        public bool IsContinueOrderOutofStock { get; set; }

        /// <summary>
        /// Quản lý kho
        /// </summary>
        public StockStatus StockStatus { get; set; }

        /// <summary>
        /// Sản phẩm
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Refer
        /// </summary>
        public virtual Product Product { get; set; }
    }
}
