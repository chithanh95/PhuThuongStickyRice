using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    public class Product : Entity<Guid>, IAggregateRoot
    {
        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Nội dung
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Mô tả ngắn
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// Trạng thái
        /// Hiển thị/ Ẩn
        /// </summary>
        public int Status { get; set; }

        #region Giá sản phẩm
        /// <summary>
        /// Giá
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Giá so sánh
        /// </summary>
        public double ComparePrice { get; set; }

        /// <summary>
        /// Bao gồm VAT
        /// </summary>
        public bool isVAT { get; set; }
        #endregion

        public Guid? CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }
}
