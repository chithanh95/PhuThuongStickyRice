using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Phone numbers subscribed to promo notifications from the landing page
    /// ("Đăng ký nhận ưu đãi"). Used for Zalo/SMS remarketing.
    /// </summary>
    public class PromoSubscriber : Entity<Guid>, IAggregateRoot
    {
        public const int PhoneMaxLength = 15;
        public const int SourceMaxLength = 50;

        public string Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Where the subscription came from (e.g. "landing").</summary>
        public string Source { get; set; } = "landing";
    }
}
