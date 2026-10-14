using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// Static policy/info page rendered at /chinh-sach/{slug}
    /// (Wave 6b, item 28). Seeded with giao-hang, doi-tra, dieu-khoan.
    /// </summary>
    public class PolicyPage : Entity<Guid>, IAggregateRoot
    {
        public const int SlugMaxLength = 100;
        public const int TitleMaxLength = 200;

        /// <summary>URL slug, unique, e.g. "giao-hang". Immutable after seeding.</summary>
        public string Slug { get; set; }

        public string Title { get; set; }

        /// <summary>Plain-text body; the frontend renders line breaks.</summary>
        public string Body { get; set; }

        public int SortOrder { get; set; }
    }
}
