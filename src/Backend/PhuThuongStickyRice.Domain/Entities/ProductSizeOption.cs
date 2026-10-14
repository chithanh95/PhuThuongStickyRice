using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Size option for a product (Nhỏ / Vừa / Lớn), each with its own
    /// absolute price in VND. A product with no options is sold at its
    /// base price only.
    /// </summary>
    public class ProductSizeOption : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 50;

        public Guid ProductId { get; set; }

        public string Name { get; set; }

        /// <summary>Absolute selling price of this size, in VND.</summary>
        public decimal Price { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
