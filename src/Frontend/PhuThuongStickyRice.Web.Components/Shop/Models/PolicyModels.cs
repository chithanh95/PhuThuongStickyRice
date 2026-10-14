using System;

namespace PhuThuongStickyRice.Web.Components.Shop.Models
{
    /// <summary>Wave 6b (item 28): policy page (/chinh-sach/{slug}).</summary>
    public class PolicyPageModel
    {
        public Guid Id { get; set; }

        public string Slug { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public int SortOrder { get; set; }
    }

    /// <summary>Wave 6b (item 28): slug + title only, for footer links.</summary>
    public class PolicyPageSummaryModel
    {
        public string Slug { get; set; }

        public string Title { get; set; }
    }

    /// <summary>Wave 6b (item 26): per-product rating aggregate.</summary>
    public class ProductRatingModel
    {
        public Guid ProductId { get; set; }

        public double AverageRating { get; set; }

        public int ReviewCount { get; set; }
    }
}
