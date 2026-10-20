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
        /// <summary>Độ dài tối đa của tên quy tắc thưởng trong DB.</summary>
        public const int NameMaxLength = 200;
        /// <summary>Độ dài tối đa của tên topping tặng kèm trong DB.</summary>
        public const int ToppingNameMaxLength = 200;

        /// <summary>Tên quy tắc hiển thị ở admin (vd "Tặng topping đơn từ 200k").</summary>
        public string Name { get; set; }

        /// <summary>Minimum order total (VND) to trigger the rule.</summary>
        public decimal MinAmount { get; set; }

        /// <summary>Hình thức thưởng: ghi chú tặng topping lên đơn hoặc sinh mã giảm giá.</summary>
        public OrderRewardType RewardType { get; set; } = OrderRewardType.FreeTopping;

        /// <summary>Free topping name (when RewardType = FreeTopping).</summary>
        public string ToppingName { get; set; }

        /// <summary>Percent of the auto-generated discount code (when RewardType = DiscountCode).</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>Tắt/bật quy tắc — quy tắc tắt không được xét khi đơn hoàn tất.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Thời điểm tạo quy tắc (UTC), dùng để sắp xếp ở màn admin.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
