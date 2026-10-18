using System;
using System.ComponentModel.DataAnnotations;

namespace PhuThuongStickyRice.Web.Components.Products.Models
{
    public class ProductModel
    {
        public Guid Id { get; set; }

        [Required]
        [MinLength(3)]
        public string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string Currency { get; set; } = "VND";

        /// <summary>Cost price (giá vốn) in VND, for the profit report.</summary>
        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        /// <summary>Item 81: selling unit — "suất" (per portion) or "kg" (per kilogram).</summary>
        public string Unit { get; set; } = "suất";

        public string Status { get; set; }

        /// <summary>Comma-separated dietary tags: "chay", "man", "cay".</summary>
        public string Tags { get; set; }

        public string Category { get; set; }

        public string AllergenInfo { get; set; }

        public bool IsNew { get; set; }

        public bool IsSignature { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        /// <summary>Item 109: optional branch this product belongs to; null = all branches.</summary>
        public Guid? BranchId { get; set; }

        public string BranchName { get; set; }

        /// <summary>
        /// Item 146: manual display order (drag &amp; drop on /products).
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Item 164: daily sale time windows as JSON, e.g.
        /// [{"from":"06:00","to":"10:00"}]. Null/empty = sold all day.
        /// </summary>
        public string AvailableHoursJson { get; set; }

        /// <summary>Item 165: max portions sold per day. Null = unlimited.</summary>
        public int? DailyLimit { get; set; }

        /// <summary>
        /// Item 166: photo gallery as a JSON string array of image URLs
        /// (first = primary).
        /// </summary>
        public string ImageUrlsJson { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
