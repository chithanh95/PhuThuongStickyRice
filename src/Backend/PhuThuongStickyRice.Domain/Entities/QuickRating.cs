using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// One-tap quick rating from the landing page — no login required.
    /// Optional product scope; aggregate view shows overall sentiment.
    /// </summary>
    public class QuickRating : Entity<Guid>, IAggregateRoot
    {
        /// <summary>1..5 stars.</summary>
        public int Rating { get; set; }

        /// <summary>Nullable: null = overall shop rating.</summary>
        public Guid? ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
