using System;
using System.Collections.Generic;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Post-delivery survey: star rating + comment for a completed order.
    /// Hidden reviews are not shown publicly; the admin can reply.
    /// </summary>
    public class OrderReview : Entity<Guid>, IAggregateRoot
    {
        public const int OrderNumberMaxLength = 30;
        public const int PhoneMaxLength = 30;
        public const int CommentMaxLength = 1000;
        public const int AdminReplyMaxLength = 1000;

        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Star rating 1-5.</summary>
        public int Rating { get; set; }

        public string Comment { get; set; }

        /// <summary>Optional photo attached to the review (uploaded by customer).</summary>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Auto-generated voucher code rewarding a photo review (1 free xoi portion).
        /// Null when the review had no photo.
        /// </summary>
        public string RewardCode { get; set; }

        /// <summary>Hidden reviews are excluded from the public listing.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Admin reply, shown publicly under the review.</summary>
        public string AdminReply { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Wave 6b (item 26): per-dish star ratings for the products of
        /// this order. Loaded explicitly where needed.
        /// </summary>
        public List<OrderReviewLine> Lines { get; set; } = new List<OrderReviewLine>();
    }
}
