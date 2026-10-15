using System;

namespace PhuThuongStickyRice.Domain.Entities
{
    /// <summary>
    /// UGC gallery photo (item 123): a customer shares a photo of the dish
    /// they received. Only approved photos appear in the public gallery.
    /// </summary>
    public class CustomerPhoto : Entity<Guid>, IAggregateRoot
    {
        public const int CaptionMaxLength = 300;
        public const int PhoneMaxLength = 30;
        public const int NameMaxLength = 100;

        /// <summary>Relative URL under /uploads/customer-photos/.</summary>
        public string ImageUrl { get; set; }

        public string Caption { get; set; }

        /// <summary>
        /// Optional phone (used by the track-order share flow). Nullable since
        /// the /check-in page only asks for a name.
        /// </summary>
        public string Phone { get; set; }

        /// <summary>Display name shown under the photo in the gallery.</summary>
        public string Name { get; set; }

        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
