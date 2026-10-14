using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Per-dish star rating inside an order review (Wave 6b, item 26).
    /// One row per rated product of the order; the parent review carries
    /// the overall rating and comment.
    /// </summary>
    public class OrderReviewLine : Entity<Guid>
    {
        public const int ProductNameMaxLength = 200;

        public Guid OrderReviewId { get; set; }

        public Guid ProductId { get; set; }

        public string ProductName { get; set; }

        /// <summary>Star rating 1-5.</summary>
        public int Rating { get; set; }

        public OrderReview OrderReview { get; set; }
    }
}
