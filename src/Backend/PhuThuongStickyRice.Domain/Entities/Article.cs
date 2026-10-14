using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// News article for the landing page "Chuyện nhà xôi" section.
    /// Replaces the hardcoded JSON news items when published articles exist.
    /// </summary>
    public class Article : Entity<Guid>, IAggregateRoot
    {
        public const int TitleMaxLength = 300;
        public const int BodyMaxLength = 10000;
        public const int ImageUrlMaxLength = 500;

        public string Title { get; set; }

        public string Body { get; set; }

        public string ImageUrl { get; set; }

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = true;
    }
}
