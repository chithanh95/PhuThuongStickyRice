using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>How an <see cref="OrderRewardRule"/> rewards the customer.</summary>
    public enum OrderRewardType
    {
        /// <summary>Adds a free-topping note on the order.</summary>
        FreeTopping = 0,

        /// <summary>Auto-generates a discount code for the customer's phone.</summary>
        DiscountCode = 1
    }

    /// <summary>
    /// Automatic reward rule evaluated when an order is completed:
    /// orders with total at or above <see cref="MinAmount"/> receive the reward.
    /// </summary>
    public class OrderRewardRule : Entity<Guid>, IAggregateRoot
    {
        public const int NameMaxLength = 200;
        public const int ToppingNameMaxLength = 200;

        public string Name { get; set; }

        /// <summary>Minimum order total (VND) to trigger the rule.</summary>
        public decimal MinAmount { get; set; }

        public OrderRewardType RewardType { get; set; } = OrderRewardType.FreeTopping;

        /// <summary>Free topping name (when RewardType = FreeTopping).</summary>
        public string ToppingName { get; set; }

        /// <summary>Percent of the auto-generated discount code (when RewardType = DiscountCode).</summary>
        public decimal? DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
