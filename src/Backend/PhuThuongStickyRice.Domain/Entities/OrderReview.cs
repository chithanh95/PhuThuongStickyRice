using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Post-delivery survey: star rating + comment for a completed order.
    /// </summary>
    public class OrderReview : Entity<Guid>, IAggregateRoot
    {
        public const int OrderNumberMaxLength = 30;
        public const int PhoneMaxLength = 30;
        public const int CommentMaxLength = 1000;

        public Guid OrderId { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerPhone { get; set; }

        /// <summary>Star rating 1-5.</summary>
        public int Rating { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
