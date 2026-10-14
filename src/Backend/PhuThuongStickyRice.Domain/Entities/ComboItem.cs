using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>One product + quantity inside a <see cref="Combo"/>.</summary>
    public class ComboItem : Entity<Guid>
    {
        public Guid ComboId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
