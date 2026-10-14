using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Optional add-on (chả lụa, trứng ốp la, hành phi...) with an extra
    /// charge. Applies to any product; the customer picks toppings per
    /// cart item in the quick-view modal.
    /// </summary>
    public class Topping : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 100;

        public string Name { get; set; }

        /// <summary>Extra charge in VND added to the item's unit price.</summary>
        public decimal ExtraPrice { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
