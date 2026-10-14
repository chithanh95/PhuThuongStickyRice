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

        /// <summary>Relative URL under /uploads/customer-photos/.</summary>
        public string ImageUrl { get; set; }

        public string Caption { get; set; }

        public string Phone { get; set; }

        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
