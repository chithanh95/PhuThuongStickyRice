using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Customer's favorite dish (item 63): saved by phone number so guests
    /// can keep a wishlist without an account. Powers the "món bạn thích
    /// đã có lại" back-in-stock notification.
    /// </summary>
    public class FavoriteDish : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 20;

        public string Phone { get; set; }

        public Guid ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
