using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Item 137: server-side shopping cart synced by user account.
    /// Items are stored as JSON (same shape as CartItemModel) for simplicity.
    /// When logged in, server is the source of truth; otherwise localStorage.
    /// </summary>
    public class ShoppingCart : Entity<Guid>, IAggregateRoot
    {
        /// <summary>User ID (from Identity) or phone number for guest-linked carts.</summary>
        public string UserId { get; set; }

        /// <summary>JSON array of cart items.</summary>
        public string ItemsJson { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
