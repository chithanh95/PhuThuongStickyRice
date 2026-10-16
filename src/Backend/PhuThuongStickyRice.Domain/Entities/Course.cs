using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Online course (item 60): e.g. "Dạy nấu xôi online".
    /// Paid video course sold on the landing page.
    /// </summary>
    public class Course : Entity<Guid>, IAggregateRoot
    {
        public const int TitleMaxLength = 200;
        public const int UrlMaxLength = 500;

        public string Title { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string ThumbnailUrl { get; set; }

        public string VideoUrl { get; set; }

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
