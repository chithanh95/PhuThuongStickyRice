using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Item 130: history of auto-posts to the Facebook Page feed
    /// (new products). Shown in admin for transparency.
    /// </summary>
    public class FacebookPostLog : Entity<Guid>, IAggregateRoot
    {
        public const int ProductNameMaxLength = 200;
        public const int PostUrlMaxLength = 500;
        public const int StatusMaxLength = 20;
        public const int ErrorMessageMaxLength = 500;

        public const string StatusPosted = "Posted";
        public const string StatusFailed = "Failed";
        public const string StatusSkipped = "Skipped";

        public Guid? ProductId { get; set; }

        public string ProductName { get; set; }

        /// <summary>Public URL of the Facebook post (null when not posted).</summary>
        public string PostUrl { get; set; }

        /// <summary>Posted / Failed / Skipped (not configured).</summary>
        public string Status { get; set; }

        public string ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
