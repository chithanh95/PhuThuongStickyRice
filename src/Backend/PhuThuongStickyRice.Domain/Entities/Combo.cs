using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Family combo: a fixed-price bundle of products + quantities
    /// (e.g. "Combo gia đình 4 người"). Ordered as a single order line
    /// at the combo price.
    /// </summary>
    public class Combo : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 1000;

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>Fixed bundle price in VND.</summary>
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ComboItem> Items { get; set; } = new List<ComboItem>();
    }
}
